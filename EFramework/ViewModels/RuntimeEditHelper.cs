using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Kendo.Mvc.Extensions;
using SpolisShared.Interfaces;

namespace SpolisShared.Helpers
{
    public static class RuntimeEditHelper
    {
        private static Dictionary<int, Dictionary<Guid, SortedSet<iModelMeta>>> Registry = new Dictionary<int, Dictionary<Guid, SortedSet<iModelMeta>>>();
        private static object LockObject = new object();

        private static Dictionary<Guid, SortedSet<iModelMeta>> GetUserDictionary(int userId)
        {
            lock (LockObject)
            {
                Dictionary<Guid, SortedSet<iModelMeta>> userDictionary = null;
                Registry.TryGetValue(userId, out userDictionary);
                if (userDictionary == null)
                {
                    userDictionary = new Dictionary<Guid, SortedSet<iModelMeta>>();
                    Registry.Add(userId, userDictionary);
                }
                return userDictionary;
            }
        }

        private static SortedSet<iModelMeta> GetReferenceCollection(int userId, Guid referenceId)
        {
            lock (LockObject)
            {
                var userDictionary = GetUserDictionary(userId);
                userDictionary.TryGetValue(referenceId, out SortedSet<iModelMeta> refCollection);
                if (refCollection == null)
                {
                    refCollection = new SortedSet<iModelMeta>(comparer: new ModelComparer());
                    userDictionary.Add(referenceId, refCollection);
                }
                return refCollection;
            }
        }

        public static void UpdateCollection(int userId, Guid referenceId, IEnumerable<iModelMeta> models)
        {
            var collection = GetReferenceCollection(userId, referenceId);
            collection.Clear();
            //collection.AddRange(models);
        }
        public static void UpdateCollectionNoClear(int userId, Guid referenceId, IEnumerable<iModelMeta> models)
        {
            var collection = GetReferenceCollection(userId, referenceId);
            //collection.AddRange(models);
        }
        public static  IEnumerable<iModelMeta> GetModels(int userId, Guid referenceId)
        {
            var collection = GetReferenceCollection(userId, referenceId);
            return collection.ToArray();
        }

        public static void AddOrReplaceModel(int userId, Guid referenceId, iModelMeta model)
        {
            var collection = GetReferenceCollection(userId, referenceId);
            if (collection.Contains(model))
            {
                collection.Remove(model);
            }
            collection.Add(model);
        }

        public static bool RemoveModel(int userId, Guid referenceId, iModelMeta model)
        {
            var collection = GetReferenceCollection(userId, referenceId);
            return collection.Remove(model);
        }

        public static void ClearCollection(int userId, Guid referenceId)
        {
            lock (LockObject)
            {
                var userDictionary = GetUserDictionary(userId);
                if (userDictionary.ContainsKey(referenceId))
                {
                    userDictionary[referenceId].Clear();
                    userDictionary.Remove(referenceId);
                }
            }
        }

        public static void ClearUser(int userId)
        {
            lock (LockObject)
            {
                if (Registry.ContainsKey(userId))
                {
                    Registry[userId].Clear();
                    Registry.Remove(userId);
                }
            }
        }

        public static bool ContainsKey(int userId, Guid referenceId)
        {
            lock (LockObject)
            {
                var userDictionary = GetUserDictionary(userId);
                return userDictionary.ContainsKey(referenceId);
            }
        }


        private class ModelComparer : IComparer<iModelMeta>
        {
            public int Compare(iModelMeta x, iModelMeta y)
            {
                if (x.Id == null || y.Id == null) throw new ArgumentException("Model id cannot be null.");
                return x.Id.Value.CompareTo(y.Id.Value);
            }
        }


    }
}
