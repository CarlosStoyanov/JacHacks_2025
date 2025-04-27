namespace SDCards.Services;

using MongoDB.Driver;
using SDCards.Models;
using MongoDB.Bson.Serialization;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDb"));
        _database = client.GetDatabase("SDCardsDb");
    }

    public IMongoCollection<DecisionRoom> DecisionRooms => _database.GetCollection<DecisionRoom>("DecisionRooms");
    public IMongoCollection<Card> Cards => _database.GetCollection<Card>("Cards");
    public IMongoCollection<Swipe> Swipes => _database.GetCollection<Swipe>("Swipes");
    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    
    public async Task<DecisionRoom> GetDecisionRoomAsync(string roomId)
    {
        return await DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
    }

}
