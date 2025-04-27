namespace SDCards.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Card
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Username { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
}