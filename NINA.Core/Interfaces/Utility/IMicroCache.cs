using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Interfaces.Utility {
    public interface IMicroCache<T> {
        bool Contains(string key);
        T GetOrAdd(string key, Func<T> loadFunction, Func<CacheItemPolicy> getCacheItemPolicyFunction);
        T GetOrAdd(string key, Func<T> loadFunction, TimeSpan timeToLive);
        void Remove(string key);
    }

    public interface IMicroCacheFactory {
        IMicroCache<T> Create<T>();
        IMicroCache<T> Create<T>(ObjectCache objectCache);
    }
}
