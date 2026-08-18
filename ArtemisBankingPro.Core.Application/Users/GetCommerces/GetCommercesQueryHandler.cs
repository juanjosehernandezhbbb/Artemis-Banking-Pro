using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Users.GetCommerces
{
    public class GetCommercesQueryHandler : IRequestHandler<GetCommercesQuery, PagedResult<CommerceUserDto>>
    {
        private readonly IIdentityService _identityService;

        public GetCommercesQueryHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<PagedResult<CommerceUserDto>> Handle(GetCommercesQuery request, CancellationToken cancellationToken)
        {
            var (users, totalRecords) = await _identityService.GetCommercesPagedAsync(request.Page, request.PageSize);

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<CommerceUserDto>(request.Page, request.PageSize, totalRecords, totalPages, users);
        }
    }
}