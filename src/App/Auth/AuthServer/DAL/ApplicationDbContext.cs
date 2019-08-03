using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Config;
using AuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.DAL
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        private readonly Connections _connections;

        public ApplicationDbContext(Connections connections)
        {
            _connections = connections;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_connections.DBConnectionString, options=>options.MigrationsHistoryTable("__EFMigrationsHistory", "auth"));

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema("auth");
            base.OnModelCreating(builder);
        }
    }
}
