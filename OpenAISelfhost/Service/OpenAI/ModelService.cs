using Microsoft.EntityFrameworkCore;
using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.Exceptions.Http;
using OpenAISelfhost.Service.Interface;

namespace OpenAISelfhost.Service.OpenAI
{
    public class ModelService : IModelService
    {
        private readonly ServiceDatabaseContext databaseContext;
        private readonly IUserService userService;
        public ModelService(
            ServiceDatabaseContext databaseContext, IUserService userService)
        {
            this.databaseContext = databaseContext;
            this.userService = userService;
        }
        public void AddModel(ChatModel model)
        {
            if(databaseContext.ChatModels.Any(m => m.Identifier == model.Identifier))
            {
                throw new DuplicatedEntryException($"Model {model.Identifier} already exists");
            }
            try
            {
                databaseContext.ChatModels.Add(model);
                databaseContext.SaveChanges();
            }catch(DbUpdateException e)
            {
                throw new UnexpectedDatabaseExcception($"Unexpected db execution error! {e.Message}");
            }
            
        }

        public void AssignModelToUser(int userId, string identifier)
        {
            if(!databaseContext.ChatModels.Any(model => model.Identifier == identifier))
            {
                throw new ModelNotFoundException($"Model with id {identifier} not found");
            }
            if(!userService.UserExists(userId))
            {
                throw new UserNotFoundException($"User with id {userId} not found");
            }
            databaseContext.UserModelAssignments.Add(new UserModelAssignment
            {
                UserId = userId,
                ModelIdentifier = identifier
            });
            databaseContext.SaveChanges();
        }

        public void DeleteModel(string identifier)
        {
            databaseContext.ChatModels.Remove(new ChatModel { Identifier = identifier });
            databaseContext.SaveChanges();
        }

        public ChatModel? GetModel(string identifier)
        {
            return databaseContext.ChatModels.Find(identifier);
        }

        public ChatModel? GetModelForUser(int userId, string identifier)
        {
            return databaseContext.UserModelAssignments
                    .Join(
                        databaseContext.ChatModels,
                        assignment => assignment.ModelIdentifier, 
                        model => model.Identifier,
                        (assignment, model) => new { assignment, model })
                    .Where(
                        assignment => 
                            assignment.assignment.UserId == userId && 
                            assignment.model.Identifier == identifier)
                    .Select(assignment => assignment.model)
                    .FirstOrDefault();
        }

        public IEnumerable<ChatModel> GetModels()
        {
            return databaseContext.ChatModels;
        }

        public IEnumerable<ChatModel> GetModelsForUser(int userId)
        {
            return databaseContext.UserModelAssignments
                .Join(
                    databaseContext.ChatModels,
                    assignment => assignment.ModelIdentifier, 
                    model => model.Identifier,
                    (assignment, model) => new { assignment, model })
                .Where(assignment => assignment.assignment.UserId == userId)
                .Select(assignment => assignment.model);
        }

        public void UnassignModelFromUser(int userId, string identifier)
        {
            databaseContext.UserModelAssignments.Remove(new UserModelAssignment
            {
                UserId = userId,
                ModelIdentifier = identifier
            });
            databaseContext.SaveChanges();
        }

        public void UpdateModel(ChatModel model)
        {
            databaseContext.ChatModels.Update(model);
            databaseContext.SaveChanges();
        }
    }
}
