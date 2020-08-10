using DAL.Models;
using Repository.InterFace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepositories
{
    public interface IUserInfoRepository : IGenericRepository<Tb_UserInfo>
    {
    }
}
