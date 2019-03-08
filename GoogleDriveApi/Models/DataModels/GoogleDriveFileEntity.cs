using System;
using System.ComponentModel.DataAnnotations;

namespace GoogleDriveApi.Models.DataModels
{
    public class GoogleDriveFileEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long? Size { get; set; }
        public long? Version { get; set; }

        [Display(Name = "Created Time")]
        public DateTime? CreatedTime { get; set; }
    }
}