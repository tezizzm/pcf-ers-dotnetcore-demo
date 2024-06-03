using CloudPlatformDemo.Models;
using EasyNetQ;

namespace CloudPlatformDemo.Services;

public class Chatroom
{
    private readonly IBus _bus;
    private readonly AppEnv _env;
    public List<Message> Messages { get; set; } = new();


    public Chatroom(IBus bus, AppEnv env)
    {
        _bus = bus;
        _env = env;
    }

    public async Task Connect()
    {
        await _bus.PubSub.SubscribeAsync<Message>(_env.InstanceName, msg => Messages.Add(msg));
    }
    public async Task Post(Message message)
    {
        await _bus.PubSub.PublishAsync(message);
    }
}

public class Message
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string User { get; set; }
    public string Text { get; set; }
}
