using CloudPlatformDemo.Models;
using CloudPlatformDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudPlatformDemo.Controllers;

public class RabbitMqController : Controller
{
    private readonly Chatroom _chatroom;
    private readonly AppEnv _env;

    public RabbitMqController(Chatroom chatroom, AppEnv env)
    {
        _chatroom = chatroom;
        _env = env;
    }



    public IActionResult Index()
    {
        return View(GetMessages(DateTime.MinValue));
    }

    private List<Message> GetMessages(DateTime from)
    {
        var messages = _chatroom.Messages.Where(x => x.Timestamp > from).ToList();
        //_chatroom.Messages.Clear();
        return messages;
    }

    public List<Message> GetLatest()
    {
        
        return GetMessages(DateTime.Now);
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromBody] Message newMessage)
    {
        if (newMessage != null)
        {
            newMessage.Id = _chatroom.Messages.Count + 1; // Assign a new ID
            await _chatroom.Post(newMessage);
            return new JsonResult(newMessage);
        }
        return BadRequest();
    }
}
