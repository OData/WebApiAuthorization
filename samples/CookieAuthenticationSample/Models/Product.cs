﻿using System.ComponentModel.DataAnnotations;

namespace ODataAuthorizationDemo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
    }
}
