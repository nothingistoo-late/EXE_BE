using AutoMapper;
using DTOs.BoxType.Respond;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class BoxTypeService : BaseService<BoxTypes, Guid>, IBoxTypeService
    {
        private readonly IMapper _mapper;

        public BoxTypeService(IMapper mapper, IGenericRepository<BoxTypes, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;

        }

        // Get by Id
        public async Task<ApiResult<BoxTypeRespondDTO>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null || entity.IsDeleted)
                    return ApiResult<BoxTypeRespondDTO>.Failure(new Exception("Không tìm thấy loại hộp!"));

                var dto = _mapper.Map<BoxTypeRespondDTO>(entity);
                return ApiResult<BoxTypeRespondDTO>.Success(dto, "Tìm thấy loại hộp!");
            }
            catch (Exception ex)
            {
                return ApiResult<BoxTypeRespondDTO>.Failure(
                    new Exception("Lỗi khi lấy loại hộp: " + ex.Message));
            }
        }

        // Get all
        public async Task<ApiResult<List<BoxTypeRespondDTO>>> GetAllAsync()
        {
            try
            {
                var data = await _repository.GetAllAsync();
                if (data == null || data.Count == 0)
                    return ApiResult<List<BoxTypeRespondDTO>>.Failure(new Exception("Không tìm thấy loại hộp nào!"));
                var valid = data.Where(x => !x.IsDeleted).ToList();
                var dto = _mapper.Map<List<BoxTypeRespondDTO>>(valid);
                return ApiResult<List<BoxTypeRespondDTO>>.Success(dto, "Lấy danh sách loại hộp thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<BoxTypeRespondDTO>>.Failure(
                    new Exception("Lỗi khi lấy danh sách: " + ex.Message));
            }
        }

        // Get by ParentID
        public async Task<ApiResult<List<BoxTypeRespondDTO>>> GetByParentIdAsync(Guid parentId)
        {
            try
            {
                var data = await _repository.GetAllAsync(x => x.ParentID == parentId && !x.IsDeleted);
                if (data == null || data.Count == 0)
                    return ApiResult<List<BoxTypeRespondDTO>>.Failure(new Exception("Không tìm thấy loại hộp nào!"));
                var dto = _mapper.Map<List<BoxTypeRespondDTO>>(data);
                return ApiResult<List<BoxTypeRespondDTO>>.Success(dto, "Lấy danh sách loại hộp theo Parent thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<BoxTypeRespondDTO>>.Failure(
                    new Exception("Lỗi khi lấy danh sách theo ParentID: " + ex.Message));
            }
        }

        // Search by Name (LIKE)
        public async Task<ApiResult<List<BoxTypeRespondDTO>>> SearchByNameAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return await GetAllAsync();

                var data = await _repository.GetAllAsync(
                    x => !x.IsDeleted && x.Name.Contains(keyword));
                if (data == null || data.Count == 0)
                    return ApiResult<List<BoxTypeRespondDTO>>.Failure(new Exception("Không tìm thấy loại hộp nào!"));
                var dto = _mapper.Map<List<BoxTypeRespondDTO>>(data);
                return ApiResult<List<BoxTypeRespondDTO>>.Success(dto, "Tìm kiếm loại hộp thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<BoxTypeRespondDTO>>.Failure(
                    new Exception("Lỗi khi tìm kiếm: " + ex.Message));
            }
        }
    }
}
