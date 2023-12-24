using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.Service.OpenAI
{
    public interface IModelService
    {
        public IEnumerable<ChatModel> GetModels();
        public IEnumerable<ChatModel> GetModelsForUser(int userId);
        public ChatModel? GetModel(string identifier);
        public ChatModel? GetModelForUser(int userId, string identifier);
        public void AddModel(ChatModel model);
        public void AssignModelToUser(int userId, string identifier);
        public void UnassignModelFromUser(int userId, string identifier);
        public void DeleteModel(string identifier);
        public void UpdateModel(ChatModel model);
    }
}
