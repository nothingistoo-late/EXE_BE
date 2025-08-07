﻿using BusinessObjects.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class UserRepository : GenericRepository<User,Guid>, IUserRepository
    {
        private readonly EXE_BE _context;

        public UserRepository(EXE_BE context) : base(context)
        {
            _context = context;
        }

        //public async Task<PagedList<User>> SearchUsersAsync(
        //string? searchTerm,
        //RoleType? role,
        //int page,
        //int size)
        //{
        //    var predicate = PredicateBuilder.True<User>()
        //        .And(u => !u.IsDeleted);

        //    if (!string.IsNullOrWhiteSpace(searchTerm))
        //        predicate = predicate.And(u =>
        //            (u.FirstName + " " + u.LastName).Contains(searchTerm.Trim()));

        //    IQueryable<User> query = _context.Users;

        //    if (role.HasValue)
        //    {
        //        var roleName = role.Value.ToString();
        //        query = from user in query
        //                join userRole in _context.UserRoles on user.Id equals userRole.UserId
        //                join r in _context.Roles on userRole.RoleId equals r.Id
        //                where r.Name == roleName
        //                select user;
        //        if (role.Value == RoleType.SchoolNurse) // Giả sử RoleType là một enum và có giá trị là Nurse
        //        {
        //            query = query.Where(u => u.StaffProfile != null);
        //        }
        //        query = query.Where(predicate);
        //    }
        //    else
        //    {
        //        query = query.Where(predicate);
        //    }

        //    query = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);

        //    var total = await query.CountAsync();
        //    var items = await query
        //        .Skip((page - 1) * size)
        //        .Take(size)
        //        .ToListAsync();

        //    return new PagedList<User>(items, total, page, size);
        //}
        public async Task<PagedList<UserDetailsDTO>> GetUserDetailsAsync(int pageNumber, int pageSize)
        {
            // Query optimization: Count and retrieve users in a single database round trip
            var query = _context.Users.AsNoTracking();

            var totalCount = await query.CountAsync();

            // Prepare efficient query for paged results
            var pagedUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    User = u,
                    u.Id
                })
                .ToListAsync();

            var userIds = pagedUsers.Select(x => x.Id).ToList();

            // Fetch all roles for these users in a single query with projection
            var userRolesDict = await _context.Set<IdentityUserRole<Guid>>()
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(
                    _context.Roles.AsNoTracking(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name }
                )
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.RoleName).ToList()
                );

            // Map to DTOs with optimized lookups
            var userDetailsList = pagedUsers.Select(u => new UserDetailsDTO
            {
                Id = u.User.Id,
                FirstName = u.User.FirstName ?? string.Empty,
                LastName = u.User.LastName ?? string.Empty,
                Email = u.User.Email ?? string.Empty,
                Gender = u.User.Gender.ToString(), 
                CreateAt = u.User.CreatedAt,
                UpdateAt = u.User.UpdatedAt,
                Roles = userRolesDict.TryGetValue(u.User.Id, out var roles) ? roles : new List<string>()
            }).ToList();

            return new PagedList<UserDetailsDTO>(userDetailsList, totalCount, pageNumber, pageSize);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email);
        }

        public async Task<User> GetUserDetailsByIdAsync(Guid id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserName == username);
        }
        public async Task<bool> ExistsAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId);
        }
        public async Task UpdateRolesAsync(User user, IEnumerable<string> roleNames)
        {
            var normalized = roleNames.Select(r => r.ToUpper()).ToList();
            var roles = await _context.Roles
                .Where(r => normalized.Contains(r.NormalizedName))
                .ToListAsync();

            // Xóa role cũ
            var old = _context.UserRoles.Where(ur => ur.UserId == user.Id);
            _context.UserRoles.RemoveRange(old);

            // Thêm role mới
            foreach (var r in roles)
                _context.UserRoles.Add(new IdentityUserRole<Guid>
                {
                    UserId = user.Id,
                    RoleId = r.Id
                });
        }
    }
}