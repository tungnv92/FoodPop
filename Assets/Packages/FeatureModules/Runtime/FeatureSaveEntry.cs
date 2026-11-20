namespace GamePackages
{
    // One saved feature inside PlayerData: the module id and its JSON state.
    // Một tính năng được lưu trong PlayerData: id module và trạng thái JSON của nó.
    [System.Serializable]
    public class FeatureSaveEntry
    {
        public string id;
        public string json;
    }
}
