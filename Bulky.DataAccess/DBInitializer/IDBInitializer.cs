using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.DBInitializer
{
    public interface IDBInitializer
    {
        //khởi tạo role cho web không hack cứng và tạo default admin
        void Initialize();
    }
}
