namespace SDCards.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Swipe
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string CardId { get; set; }
    public string UserId { get; set; }
    public bool IsRightSwipe { get; set; }
}
