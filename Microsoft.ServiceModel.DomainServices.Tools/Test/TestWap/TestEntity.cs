using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TestWap
{
    public class TestEntity
    {
        [Key]
        public string TheKey { get; set; }
    }
}