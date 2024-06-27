using RLNET;
using RoguelikeGame.Color;
using System.Collections.Generic;

namespace RoguelikeGame.Systems.Message
{
    // Represents a queue of messages that can be added to
    // Has a method for and drawing to an RLConsole
    public class MessageLog : Singleton<MessageLog>
    {
        // Define the maximum number of lines to store
        private static readonly int _maxLines = 9;

        // Use a Queue to keep track of the lines of text
        // The first line added to the log will also be the first removed
        private readonly Queue<(string, RLColor)> _lines;

        public MessageLog()
        {
            _lines = new Queue<(string, RLColor)>();
        }

        // Add a line to the MessageLog queue
        public void Add(string message)
        {
            _lines.Enqueue((message, Colors.Text));

            // When exceeding the maximum number of lines remove the oldest one.
            if (_lines.Count > _maxLines)
            {
                _lines.Dequeue();
            }
        }
        public void Add(string message, RLColor color)
        {
            _lines.Enqueue((message, color));

            // When exceeding the maximum number of lines remove the oldest one.
            if (_lines.Count > _maxLines)
            {
                _lines.Dequeue();
            }
        }

        // Draw each line of the MessageLog queue to the console
        public void Draw(RLConsole console)
        {
            console.Clear();
            (string, RLColor)[] lines = _lines.ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                console.Print(1, i + 1, lines[i].Item1, lines[i].Item2);
            }
        }
    }
}
