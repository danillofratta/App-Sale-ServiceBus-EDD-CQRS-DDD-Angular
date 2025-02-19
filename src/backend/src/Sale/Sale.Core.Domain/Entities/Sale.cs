﻿using Base.Core.Domain.Common;
using Sale.Core.Domain.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleCoreDomainEntities
{
    public class Sale : BaseEntity
    {
        /// <summary>
        /// It is generated by the database, but can be modified by the user later.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Number { get; set; }

        /// <summary>
        /// External Identities
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// External Identities
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// External Identities
        /// </summary>
        public string BranchId { get; set; }

        /// <summary>
        /// External Identities
        /// </summary>
        public string BranchName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public SaleStatus Status { get; set; } = SaleStatus.Create;
        public List<SaleItens> SaleItens { get; set; } = new()!;
    }
}

