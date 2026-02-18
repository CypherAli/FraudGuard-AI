using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Manages a whitelist of trusted contacts
    /// Numbers in the whitelist bypass fraud detection analysis
    /// </summary>
    public class ContactWhitelistService
    {
        private const string WHITELIST_KEY = "contact_whitelist";
        private HashSet<string> _whitelist;
        private static ContactWhitelistService _instance;
        private static readonly object _lock = new();

        public static ContactWhitelistService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ContactWhitelistService();
                    }
                }
                return _instance;
            }
        }

        private ContactWhitelistService()
        {
            _whitelist = new HashSet<string>();
            _ = LoadWhitelistAsync();
        }

        /// <summary>
        /// Adds a phone number to the whitelist
        /// </summary>
        public async Task AddNumberAsync(string phoneNumber)
        {
            string normalized = NormalizePhone(phoneNumber);
            if (string.IsNullOrEmpty(normalized)) return;

            _whitelist.Add(normalized);
            await SaveWhitelistAsync();
            System.Diagnostics.Debug.WriteLine($"[Whitelist] Added: {normalized}");
        }

        /// <summary>
        /// Removes a phone number from the whitelist
        /// </summary>
        public async Task RemoveNumberAsync(string phoneNumber)
        {
            string normalized = NormalizePhone(phoneNumber);
            _whitelist.Remove(normalized);
            await SaveWhitelistAsync();
            System.Diagnostics.Debug.WriteLine($"[Whitelist] Removed: {normalized}");
        }

        /// <summary>
        /// Checks if a phone number is whitelisted (trusted)
        /// </summary>
        public bool IsWhitelisted(string phoneNumber)
        {
            string normalized = NormalizePhone(phoneNumber);
            return _whitelist.Contains(normalized);
        }

        /// <summary>
        /// Gets all whitelisted phone numbers
        /// </summary>
        public List<string> GetAll()
        {
            return _whitelist.ToList();
        }

        /// <summary>
        /// Gets the count of whitelisted numbers
        /// </summary>
        public int Count => _whitelist.Count;

        /// <summary>
        /// Imports contacts from device phonebook (Android READ_CONTACTS permission required)
        /// </summary>
        public async Task<int> ImportFromContactsAsync()
        {
            int imported = 0;

#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;
                var resolver = context.ContentResolver;
                var uri = global::Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentUri;

                string[] projection = { global::Android.Provider.ContactsContract.CommonDataKinds.Phone.Number };

                using var cursor = resolver.Query(uri, projection, null, null, null);
                if (cursor != null)
                {
                    int phoneIndex = cursor.GetColumnIndex(
                        global::Android.Provider.ContactsContract.CommonDataKinds.Phone.Number);

                    while (cursor.MoveToNext())
                    {
                        string phone = cursor.GetString(phoneIndex);
                        if (!string.IsNullOrWhiteSpace(phone))
                        {
                            string normalized = NormalizePhone(phone);
                            if (!string.IsNullOrEmpty(normalized) && !_whitelist.Contains(normalized))
                            {
                                _whitelist.Add(normalized);
                                imported++;
                            }
                        }
                    }
                }

                if (imported > 0)
                {
                    await SaveWhitelistAsync();
                }

                System.Diagnostics.Debug.WriteLine($"[Whitelist] Imported {imported} contacts from phonebook");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Whitelist] Import error: {ex.Message}");
            }
#endif

            return imported;
        }

        /// <summary>
        /// Clears all whitelisted numbers
        /// </summary>
        public async Task ClearAllAsync()
        {
            _whitelist.Clear();
            await SaveWhitelistAsync();
        }

        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

            // Remove spaces, dashes, parentheses
            string cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Convert +84 to 0
            if (cleaned.StartsWith("+84"))
                cleaned = "0" + cleaned.Substring(3);

            // Must be 10-11 digits starting with 0
            if (cleaned.Length >= 10 && cleaned.Length <= 11 && cleaned.StartsWith("0"))
                return cleaned;

            return cleaned; // Return as-is if not a Vietnamese number format
        }

        private async Task SaveWhitelistAsync()
        {
            try
            {
                string json = JsonSerializer.Serialize(_whitelist.ToList());
                await SecureStorage.Default.SetAsync(WHITELIST_KEY, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Whitelist] Save error: {ex.Message}");
            }
        }

        private async Task LoadWhitelistAsync()
        {
            try
            {
                string json = await SecureStorage.Default.GetAsync(WHITELIST_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                    {
                        _whitelist = new HashSet<string>(list);
                        System.Diagnostics.Debug.WriteLine($"[Whitelist] Loaded {_whitelist.Count} numbers");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Whitelist] Load error: {ex.Message}");
                _whitelist = new HashSet<string>();
            }
        }
    }
}
