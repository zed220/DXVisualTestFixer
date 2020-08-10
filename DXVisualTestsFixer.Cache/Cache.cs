using System;
using System.Collections.Generic;
using DXVisualTestFixer.Common;

namespace DXVisualTestsFixer.Cache {
    public class Cache<T> : ICache<T> {
        readonly Dictionary<string, T> _Cache = new Dictionary<string, T>(8192);
        
        public T GetOrAdd(byte[] sha256, Func<T> getValue) {
            if(sha256 == null)
                return default;
            var sha256Base64 = Convert.ToBase64String(sha256);
            if(_Cache.TryGetValue(sha256Base64, out var result))
                return result;
            return _Cache[sha256Base64] = getValue();
        }
    }
}