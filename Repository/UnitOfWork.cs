using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Repository.InterFace;
using Repository.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        private UserInfoRepository _userInfoRepository;

        public UserInfoRepository UserInfoRepo
        {
            get
            {
                if (_userInfoRepository == null)
                {
                    _userInfoRepository = new UserInfoRepository(_dbContext);
                }
                return _userInfoRepository;
            }
        }

         private UserActivitiesRepository _userActivitiesRepository;

        public UserActivitiesRepository UserActivitiesRepo
        {
            get
            {
                if (_userActivitiesRepository == null)
                {
                    _userActivitiesRepository = new UserActivitiesRepository(_dbContext);
                }
                return _userActivitiesRepository;
            }
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }
        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

    }
}
