﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Registry.Common;

namespace Registry.Web.Data.Models
{
    public class Dataset
    {
        [MaxLength(128)]
        [Required]
        public string Slug { get; set; }
        public Guid InternalRef { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public Organization Organization { get; set; }

        public virtual ICollection<Batch> Batches { get; set; }

        public virtual ICollection<DownloadPackage> DownloadPackages { get; set; }

    }
}