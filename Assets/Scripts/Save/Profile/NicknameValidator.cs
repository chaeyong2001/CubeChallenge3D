using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CubeChallenge3D.Save.Profile
{
    public enum NicknameValidationError
    {
        None,
        Empty,
        TooLong,
        InvalidCharacters,
        BannedWord,
        ReservedName
    }

    public readonly struct NicknameValidationResult
    {
        public NicknameValidationResult(bool isValid, string normalizedNickname, NicknameValidationError error, string message)
        {
            IsValid = isValid;
            NormalizedNickname = normalizedNickname;
            Error = error;
            Message = message;
        }

        public bool IsValid { get; }
        public string NormalizedNickname { get; }
        public NicknameValidationError Error { get; }
        public string Message { get; }
    }

    public static class NicknameValidator
    {
        private const int MaxLength = 15;
        private static readonly Regex AllowedCharacters = new Regex(@"^[A-Za-z0-9\uAC00-\uD7A3]+$", RegexOptions.Compiled);
        private static readonly string[] ReservedNames = { "admin", "moderator", "system", "player", "guest", "null" };

        private static readonly string[] BannedWords =
        {
            "fuck", "shit", "bitch", "bastard", "asshole", "dick", "pussy", "cunt",
            "sex", "porn", "nude", "nazi",
            "\uC2DC\uBC1C", "\uC2F8\uBC1C", "\uBCD1\uC2E0", "\uC9C0\uB784",
            "\uC880\uC880", "\uB2C8\uBBF8", "\uC139\uC2A4", "\uC57C\uB3D9"
        };

        public static NicknameValidationResult Validate(string input)
        {
            string normalized = Normalize(input);
            if (string.IsNullOrEmpty(normalized))
            {
                return Invalid(normalized, NicknameValidationError.Empty, "Enter a nickname.");
            }

            if (normalized.Length > MaxLength)
            {
                return Invalid(normalized, NicknameValidationError.TooLong, "Nickname must be 15 characters or less.");
            }

            if (!AllowedCharacters.IsMatch(normalized))
            {
                return Invalid(normalized, NicknameValidationError.InvalidCharacters, "Use only English, Korean, and numbers.");
            }

            string comparable = normalized.ToLowerInvariant();
            foreach (string reserved in ReservedNames)
            {
                if (comparable == reserved)
                {
                    return Invalid(normalized, NicknameValidationError.ReservedName, "This nickname is reserved.");
                }
            }

            foreach (string banned in BannedWords)
            {
                if (!string.IsNullOrEmpty(banned) && comparable.Contains(banned.ToLowerInvariant()))
                {
                    return Invalid(normalized, NicknameValidationError.BannedWord, "Choose a different nickname.");
                }
            }

            return new NicknameValidationResult(true, normalized, NicknameValidationError.None, string.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static NicknameValidationResult Invalid(string nickname, NicknameValidationError error, string message)
        {
            return new NicknameValidationResult(false, nickname, error, message);
        }
    }
}
