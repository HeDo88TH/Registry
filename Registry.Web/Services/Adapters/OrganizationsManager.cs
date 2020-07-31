﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Registry.Web.Data;
using Registry.Web.Exceptions;
using Registry.Web.Models.DTO;
using Registry.Web.Services.Ports;

namespace Registry.Web.Services.Adapters
{
    public class OrganizationsManager : IOrganizationsManager
    {
        private readonly IAuthManager _authManager;
        private readonly RegistryContext _context;
        private readonly IUtils _utils;
        private readonly IDatasetsManager _datasetManager;
        private readonly ILogger<OrganizationsManager> _logger;

        // TODO: Add extensive logging
        // TODO: Add extensive testing
        public OrganizationsManager(
            IAuthManager authManager,
            RegistryContext context,
            IUtils utils,
            IDatasetsManager datasetManager,
            ILogger<OrganizationsManager> logger)
        {
            _authManager = authManager;
            _context = context;
            _utils = utils;
            _datasetManager = datasetManager;
            _logger = logger;
        }

        public async Task<IEnumerable<OrganizationDto>> List()
        {
            var query = from org in _context.Organizations select org;

            if (!await _authManager.IsUserAdmin())
            {
                var currentUser = await _authManager.GetCurrentUser();

                if (currentUser == null)
                    throw new UnauthorizedException("Invalid user");

                query = query.Where(item => item.OwnerId == currentUser.Id || item.OwnerId == null || item.IsPublic);
            }

            return from org in query
                select new OrganizationDto
                {
                    CreationDate = org.CreationDate,
                    Description = org.Description,
                    Id = org.Id,
                    Name = org.Name,
                    Owner = org.OwnerId,
                    IsPublic = org.IsPublic
                };
        }

        public async Task<OrganizationDto> Get(string id)
        {
            var org = await _utils.GetOrganizationAndCheck(id);

            return org.ToDto();
        }

        public async Task<OrganizationDto> AddNew(OrganizationDto organization)
        {
            // TODO: To change when implementing anonymous users
            var currentUser = await _authManager.GetCurrentUser();

            if (currentUser == null)
                throw new UnauthorizedException("Invalid user");

            if (!_utils.IsOrganizationNameValid(organization.Id))
                throw new BadRequestException("Invalid organization id");

            var existingOrg = _context.Organizations.FirstOrDefault(item => item.Id == organization.Id);

            if (existingOrg != null)
                throw new ConflictException("The organization already exists");

            if (!await _authManager.IsUserAdmin())
            {

                // If the owner is specified it should be the current user
                if (organization.Owner != null && organization.Owner != currentUser.Id)
                    throw new UnauthorizedException("Cannot create a new organization that belongs to a different user");

                // The current user is the owner
                organization.Owner = currentUser.Id;

            }
            else
            {
                // If no owner specified, the owner is the current user
                if (organization.Owner == null)
                    organization.Owner = currentUser.Id;
                else
                {
                    // Otherwise check if user exists
                    if (!await _authManager.UserExists(organization.Owner))
                        throw new BadRequestException($"Cannot find user with id '{organization.Owner}'");

                }
            }

            var org = organization.ToEntity();
            org.CreationDate = DateTime.Now;

            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();

            return org.ToDto();
        }

        public async Task Edit(string id, OrganizationDto organization)
        {

            var org = await _utils.GetOrganizationAndCheck(id);

            // TODO: To change when implementing anonymous users
            var currentUser = await _authManager.GetCurrentUser();

            // NOTE: Is this a good idea? If activated there will be no way to change the public organization details
            // if (organization.Id == MagicStrings.PublicOrganizationId)
            //    return Unauthorized(new ErrorResponse("Cannot edit the public organization"));
            
            if (!await _authManager.IsUserAdmin())
            {

                // If the owner is specified it should be the current user
                if (organization.Owner != null && organization.Owner != currentUser.Id)
                    throw new UnauthorizedException("Cannot create a new organization that belongs to a different user");

                // The current user is the owner
                organization.Owner = currentUser.Id;

            }
            else
            {
                // If no owner specified, the owner is the current user
                if (organization.Owner == null)
                    organization.Owner = currentUser.Id;
                else
                {
                    // Otherwise check if user exists
                    if (!await _authManager.UserExists(organization.Owner))
                        throw new BadRequestException($"Cannot find user with id '{organization.Owner}'");

                }
            }

            org.IsPublic = organization.IsPublic;
            org.Name = organization.Name;
            org.Description = organization.Description;

            await _context.SaveChangesAsync();

        }

        public async Task Delete(string id)
        {

            var org = await _utils.GetOrganizationAndCheck(id);

            if (org == null)
                throw new NotFoundException("Cannot find organization with this id");

            if (!await _authManager.IsUserAdmin())
            {
                var currentUser = await _authManager.GetCurrentUser();

                if (org.OwnerId != currentUser.Id)
                    throw new UnauthorizedException("The current user is not the owner of the organization");
            }
            else
            {
                if (org.Id == MagicStrings.PublicOrganizationId)
                    throw new UnauthorizedException("Cannot remove the default public organization");
            }

            foreach (var ds in org.Datasets.ToArray())
            {

                // TODO: This is not the right method, we should have another abstraction and / or a direct access to IObjectSystem
                // await _datasetManager.Delete(org.Id, ds.Slug);
                _context.Datasets.Remove(ds);
            }

            _context.Organizations.Remove(org);
            await _context.SaveChangesAsync();
        }
    }
}