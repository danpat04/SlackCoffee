namespace SlackCoffee.Utils
{
    public abstract class SlackResponse
    {
    }

    public class SimpleResponse : SlackResponse
    {
        public string response_type { get; set; }
        public string text { get; set; }

        public static SimpleResponse Ephemeral(string text_)
        {
            return new SimpleResponse { response_type = "ephemeral", text = text_ };
        }

        public static SimpleResponse InChannel(string text_)
        {
            return new SimpleResponse { response_type = "in_channel", text = text_ };
        }
    }
}
