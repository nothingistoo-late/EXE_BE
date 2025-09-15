using DTOs.BoxType.Respond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IBoxTypeService
    {
        Task<ApiResult<BoxTypeRespondDTO>> GetByIdAsync(Guid id);
        Task<ApiResult<List<BoxTypeRespondDTO>>> GetAllAsync();
        Task<ApiResult<List<BoxTypeRespondDTO>>> GetByParentIdAsync(Guid parentId);
        Task<ApiResult<List<BoxTypeRespondDTO>>> SearchByNameAsync(string keyword);
    }

}
