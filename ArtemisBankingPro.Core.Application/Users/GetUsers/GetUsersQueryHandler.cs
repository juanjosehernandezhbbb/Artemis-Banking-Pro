using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Users.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        private readonly IIdentityService _identityService;

        public GetUsersQueryHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var (users, totalRecords) = await _identityService.GetUsersPagedAsync(request.Page, request.PageSize, request.Role);

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<UserDto>(request.Page, request.PageSize, totalRecords, totalPages, users);
        }
    }
}