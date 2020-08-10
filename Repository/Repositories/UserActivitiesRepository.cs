using DAL;
using DAL.Models;
using Repository.IRepositories;

namespace Repository.Repositories
{
    public class UserActivitiesRepository : GenericRepositori<Tb_UserActivities>, IUserActivitiesRepository
    {
        public UserActivitiesRepository(ApplicationDbContext Db) : base(Db)
        { }


       
    }
}
