using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.DAL
{
    public class AppContextMigrator: IStartable
    {
        private ApplicationDbContext _context;

        public AppContextMigrator(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Start()
        {
            _context.Database.Migrate();
        }
    }
}
