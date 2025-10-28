using AutoMapper;
using DTOs.BoxType.Request;
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
                {
                    // Nothing to do if we're only keeping Search — return empty or message
                    return ApiResult<List<BoxTypeRespondDTO>>.Failure(new Exception("Keyword is required"));
                }

                var data = await _repository.GetAllAsync(x => !x.IsDeleted && x.Name.Contains(keyword));
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

        // Update
        // Update: only update fields that are provided in request
        public async Task<ApiResult<BoxTypeRespondDTO>> UpdateAsync(Guid id, UpdateBoxTypeRequest request)
        {
            try
            {
                var entity = await _unitOfWork.BoxTypeRepository.GetByIdAsync(id);
                if (entity == null || entity.IsDeleted)
                    return ApiResult<BoxTypeRespondDTO>.Failure(new Exception("Không tìm thấy loại hộp!"));

                // Determine the new values that would result from this update for duplicate check
                var intendedName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : entity.Name;
                var intendedParentId = request.ParentID.HasValue ? request.ParentID.Value : entity.ParentID;

                // Only check duplicate when Name or ParentID is going to change
                if ((!string.Equals(intendedName, entity.Name, StringComparison.Ordinal))
                    || intendedParentId != entity.ParentID)
                {
                    var duplicate = await _unitOfWork
                        .BoxTypeRepository
                        .AnyAsync(x => !x.IsDeleted
                                       && x.Name == intendedName
                                       && x.ParentID == intendedParentId
                                       && x.Id != id);

                    if (duplicate)
                        return ApiResult<BoxTypeRespondDTO>.Failure(new Exception("Tên loại hộp đã tồn tại trong nhóm này!"));
                }

                // Apply partial updates
                if (!string.IsNullOrWhiteSpace(request.Name))
                    entity.Name = request.Name.Trim();

                if (!string.IsNullOrWhiteSpace(request.Description))
                    entity.Description = request.Description.Trim();

                if (request.Price.HasValue)
                    entity.Price = request.Price.Value;

                if (request.ParentID.HasValue)
                    entity.ParentID = request.ParentID.Value;

                await base.UpdateAsync(entity);

                var dto = _mapper.Map<BoxTypeRespondDTO>(entity);
                return ApiResult<BoxTypeRespondDTO>.Success(dto, "Cập nhật loại hộp thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<BoxTypeRespondDTO>.Failure(new Exception("Lỗi khi cập nhật loại hộp: " + ex.Message));
            }
        }
        // Delete (soft)
        public async Task<ApiResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var exists = await _unitOfWork.BoxTypeRepository.AnyAsync(x => x.Id == id && !x.IsDeleted);
                if (!exists)
                    return ApiResult<bool>.Failure(new Exception("Không tìm thấy loại hộp!"));

                var ok = await base.DeleteAsync(id);
                return ok
                    ? ApiResult<bool>.Success(true, "Xóa mềm loại hộp thành công!")
                    : ApiResult<bool>.Failure(new Exception("Xóa mềm loại hộp thất bại!"));
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception("Lỗi khi xóa loại hộp: " + ex.Message));
            }
        }
    }
}
