using System.Threading.Tasks;
using UnityEditor;

namespace DependenciesGraph
{
    public static class AssetDatabaseAsync
    {
        public static TaskScheduler MainThread;

        public static Task<string[]> FindAssets(string filter, string[] searchInFolders)
        {
            var task = new Task<string[]>(() => AssetDatabase.FindAssets(filter, searchInFolders));
            task.Start(MainThread);
            return task;
        }

        public static Task<string> GUIDToAssetPath(string guid)
        {
            var task = new Task<string>(() => AssetDatabase.GUIDToAssetPath(guid));
            task.Start(MainThread);
            return task;
        }

        public static Task<string[]> GetDependencies(string pathName, bool recursive)
        {
            var task = new Task<string[]>(() => AssetDatabase.GetDependencies(pathName, recursive));
            task.Start(MainThread);
            return task;
        }
    }
}