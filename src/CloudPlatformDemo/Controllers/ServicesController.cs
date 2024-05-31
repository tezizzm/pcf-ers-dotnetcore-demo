using CloudPlatformDemo.Models;
using CloudPlatformDemo.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudPlatformDemo.Controllers;

public class ServicesController : Controller
{
    private readonly AttendeeContext _db;

    public ServicesController(AttendeeContext db)
    {
        _db = db;
    }

    public IActionResult Database() => View();
    public IActionResult RabbitMq() => View();
        
    [HttpPost]
    public async Task ClearUsers()
    {
        var attendees = await _db.Attendees.ToListAsync();
        _db.Attendees.RemoveRange(attendees);
        await _db.SaveChangesAsync();
    }

    [HttpPost]
    public async Task AddUser(Attendee attendee)
    {

        _db.Attendees.Add(attendee);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Attendee>> GetUsers() => await _db.Attendees.ToListAsync();

    public DbConnectionInfo GetDbConnectionInfo() => new()
    {
        ProviderName = _db.Database.ProviderName, 
        ConnectionString = _db.Database.GetConnectionString()
    };
}