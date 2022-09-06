﻿using System.Threading.Tasks;
using Registry.Web.Data.Models;
using Registry.Web.Identity.Models;
using Registry.Web.Models;
using Registry.Web.Models.DTO;

namespace Registry.Web.Services.Ports
{
    public interface IUtils
    {
        Organization GetOrganization(string orgSlug, bool safe = false);
        Dataset GetDataset(string orgSlug, string dsSlug, bool safe = false);

        string GetFreeOrganizationSlug(string orgName);
        string GenerateDatasetUrl(Dataset dataset);
        
        UserStorageInfo GetUserStorage(User user);
        Task CheckCurrentUserStorage(long size = 0);

    }
}
