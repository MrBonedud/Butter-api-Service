using System.Security.Cryptography;
using Core.Interfaces;

namespace Infrastructure.Rooms;

public class RoomCodeGenerator : IRoomCodeGenerator
{
    private const int CodeLength = 6;
    private const string AllowedCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Generate()
    {
        var characters = new char[CodeLength];

        for (var index = 0; index < CodeLength; index++)
        {
            var randomIndex =
                RandomNumberGenerator.GetInt32(AllowedCharacters.Length);

            characters[index] = AllowedCharacters[randomIndex];
        }

        return new string(characters);
    }
}