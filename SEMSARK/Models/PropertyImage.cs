﻿using System.ComponentModel.DataAnnotations.Schema;

namespace SEMSARK.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        public Property Property { get; set; }
    }
}
