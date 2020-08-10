using DAL;
using DAL.Models;
using Repository.IRepositories;

namespace Repository.Repositories
{
    public class UserInfoRepository : GenericRepositori<Tb_UserInfo>, IUserInfoRepository
    {
        public UserInfoRepository(ApplicationDbContext Db) : base(Db)
        { }


       
    }
}
