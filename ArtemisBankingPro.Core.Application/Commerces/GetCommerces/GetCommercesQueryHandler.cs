using ArtemisBankingPro.Core.Application.Common.Models;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerces
{
    public class GetCommercesQueryHandler : IRequestHandler<GetCommercesQuery, PagedResult<CommerceListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCommercesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<CommerceListItemDto>> Handle(GetCommercesQuery request, CancellationToken cancellationToken)
        {
            var comercios = (await _unitOfWork.Repository<Comercio>().FindAsync(_ => true)).ToList();

            var status = request.Status?.ToLower() ?? "activo";
            if (status != "todos")
            {
                var expected = status == "activo";
                comercios = comercios.Where(c => c.IsActive == expected).ToList();
            }

            comercios = comercios.OrderByDescending(c => c.FechaCreacion).ToList();

            var totalRecords = comercios.Count;
            var paged = comercios.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var items = paged.Select(c => new CommerceListItemDto(
                c.Id,
                c.Name,
                c.Description,
                c.Email,
                c.PhoneNumber,
                c.Rnc,
                c.IsActive,
                c.ApplicationUserId != null,
                c.FechaCreacion
            )).ToList();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

            return new PagedResult<CommerceListItemDto>(request.Page, request.PageSize, totalRecords, totalPages, items);
        }
    }
}