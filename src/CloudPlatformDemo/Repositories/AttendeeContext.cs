using CloudPlatformDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudPlatformDemo.Repositories;

public class AttendeeContext : DbContext
{
    protected AttendeeContext()
    {
    }

    public AttendeeContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Attendee> Attendees { get; set; }
}