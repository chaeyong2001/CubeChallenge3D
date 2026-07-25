from dataclasses import dataclass
from typing import Optional


@dataclass(frozen=True)
class IapProductConfig:
    product_id: str
    product_type: str
    grant_currency_type: Optional[str] = None
    grant_amount: int = 0
    entitlement_key: Optional[str] = None


PRODUCTS: dict[str, IapProductConfig] = {
    "gem_pack_small": IapProductConfig(
        product_id="gem_pack_small",
        product_type="consumable",
        grant_currency_type="gems",
        grant_amount=80,
    ),
    "gem_pack_medium": IapProductConfig(
        product_id="gem_pack_medium",
        product_type="consumable",
        grant_currency_type="gems",
        grant_amount=450,
    ),
    "gem_pack_large": IapProductConfig(
        product_id="gem_pack_large",
        product_type="consumable",
        grant_currency_type="gems",
        grant_amount=800,
    ),
    "remove_ads": IapProductConfig(
        product_id="remove_ads",
        product_type="non_consumable",
        entitlement_key="remove_ads",
    ),
}


def get_product(product_id: str) -> IapProductConfig | None:
    return PRODUCTS.get((product_id or "").strip())
