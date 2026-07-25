import json
import time
from dataclasses import dataclass
from typing import Any

from app.config import settings


class GooglePlayApiError(RuntimeError):
    def __init__(self, error_code: str, message: str):
        super().__init__(message)
        self.error_code = error_code
        self.message = message


@dataclass
class GooglePurchaseInfo:
    raw: dict[str, Any]
    purchase_state: int | None
    acknowledgement_state: int | None
    consumption_state: int | None
    order_id: str | None


class GooglePlayDeveloperApi:
    android_publisher_scope = "https://www.googleapis.com/auth/androidpublisher"

    def __init__(self):
        self._credentials = None

    def get_product_purchase(self, package_name: str, product_id: str, purchase_token: str) -> GooglePurchaseInfo:
        response = self._request_json(
            "GET",
            f"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package_name}/purchases/products/{product_id}/tokens/{purchase_token}",
        )
        return GooglePurchaseInfo(
            raw=response,
            purchase_state=_to_int(response.get("purchaseState")),
            acknowledgement_state=_to_int(response.get("acknowledgementState")),
            consumption_state=_to_int(response.get("consumptionState")),
            order_id=response.get("orderId"),
        )

    def acknowledge_product_purchase(self, package_name: str, product_id: str, purchase_token: str) -> None:
        self._request_json(
            "POST",
            f"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package_name}/purchases/products/{product_id}/tokens/{purchase_token}:acknowledge",
            body={},
        )

    def consume_product_purchase(self, package_name: str, product_id: str, purchase_token: str) -> None:
        self._request_json(
            "POST",
            f"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package_name}/purchases/products/{product_id}/tokens/{purchase_token}:consume",
            body={},
        )

    def list_voided_purchases(self, package_name: str) -> list[dict[str, Any]]:
        now_ms = int(time.time() * 1000)
        response = self._request_json(
            "GET",
            f"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package_name}/purchases/voidedpurchases?endTime={now_ms}",
        )
        return response.get("voidedPurchases", []) or []

    def _request_json(self, method: str, url: str, body: dict[str, Any] | None = None) -> dict[str, Any]:
        credentials = self._get_credentials()
        credentials.refresh(_google_auth_request())
        headers = {
            "Authorization": f"Bearer {credentials.token}",
            "Accept": "application/json",
        }
        data = None
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"

        from urllib import request as urllib_request
        from urllib import error as urllib_error

        req = urllib_request.Request(url, data=data, method=method, headers=headers)
        try:
            with urllib_request.urlopen(req, timeout=15) as response:
                text = response.read().decode("utf-8")
        except urllib_error.HTTPError as exc:
            details = exc.read().decode("utf-8", errors="replace")
            raise GooglePlayApiError("GOOGLE_API_HTTP_ERROR", f"Google Play API HTTP {exc.code}: {details}") from exc
        except Exception as exc:
            raise GooglePlayApiError("GOOGLE_API_UNAVAILABLE", str(exc)) from exc

        return json.loads(text) if text else {}

    def _get_credentials(self):
        if self._credentials is not None:
            return self._credentials

        try:
            from google.oauth2 import service_account
        except Exception as exc:
            raise GooglePlayApiError(
                "GOOGLE_AUTH_LIBRARY_MISSING",
                "google-auth is not installed on the server.",
            ) from exc

        if settings.google_play_service_account_json:
            try:
                info = json.loads(settings.google_play_service_account_json)
            except json.JSONDecodeError as exc:
                raise GooglePlayApiError("GOOGLE_CREDENTIALS_INVALID", "Invalid service account JSON.") from exc
            self._credentials = service_account.Credentials.from_service_account_info(
                info,
                scopes=[self.android_publisher_scope],
            )
            return self._credentials

        if settings.google_play_service_account_file:
            self._credentials = service_account.Credentials.from_service_account_file(
                settings.google_play_service_account_file,
                scopes=[self.android_publisher_scope],
            )
            return self._credentials

        raise GooglePlayApiError(
            "GOOGLE_CREDENTIALS_MISSING",
            "Google Play service account credentials are not configured.",
        )


def _google_auth_request():
    try:
        from google.auth.transport.requests import Request
    except Exception as exc:
        raise GooglePlayApiError(
            "GOOGLE_AUTH_LIBRARY_MISSING",
            "google-auth transport is not installed on the server.",
        ) from exc
    return Request()


def _to_int(value: Any) -> int | None:
    try:
        return int(value)
    except (TypeError, ValueError):
        return None
