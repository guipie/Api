/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果数据库字段发生变化，请在代码生器重新生成此Model
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monster.Entity.SystemModels;

namespace Monster.Entity.DomainModels
{
    [Entity(TableCnName = "资讯类别", TableName = "news_type")]
    [Table("news_type")]
    public class NewsType : BaseEntity
    {
        /// <summary>
        ///创建人Id
        /// </summary>
        [Display(Name = "创建人Id")]
        [Column(TypeName = "int")]
        [Required(AllowEmptyStrings = false)]
        public int CreateID { get; set; }

        /// <summary>
        ///创建人
        /// </summary>
        [Display(Name = "创建人")]
        [MaxLength(30)]
        [Column(TypeName = "nvarchar(30)")]
        [Required(AllowEmptyStrings = false)]
        public string Creator { get; set; }

        /// <summary>
        ///申请时间
        /// </summary>
        [Display(Name = "申请时间")]
        [Column(TypeName = "datetime")]
        [Required(AllowEmptyStrings = false)]
        public DateTime CreateDate { get; set; }

        /// <summary>
        ///修改人ID
        /// </summary>
        [Display(Name = "修改人ID")]
        [Column(TypeName = "int")]
        public int? ModifyID { get; set; }

        /// <summary>
        ///修改人
        /// </summary>
        [Display(Name = "修改人")]
        [MaxLength(30)]
        [Column(TypeName = "nvarchar(30)")]
        public string Modifier { get; set; }

        /// <summary>
        ///修改时间
        /// </summary>
        [Display(Name = "修改时间")]
        [Column(TypeName = "datetime")]
        public DateTime? ModifyDate { get; set; }


        /// <summary>
        ///
        /// </summary>
        [Display(Name = "IsHidden")]
        [Column(TypeName = "smallint")]
        public bool IsHidden { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "Description")]
        [MaxLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string Description { get; set; }


        [Display(Name = "Sequence")]
        [Column(TypeName = "short")]
        [Editable(true)]
        [Required(AllowEmptyStrings = false)]
        public short Sequence { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "Code")]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        [Required(AllowEmptyStrings = false)]
        [Editable(true)]
        public string Code { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Display(Name = "Name")]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        [Required(AllowEmptyStrings = false)]
        [Editable(true)]
        public string Name { get; set; }

        [Display(Name = "Pid")]
        [Column(TypeName = "int")]
        [Required(AllowEmptyStrings = false)]
        public int Pid { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Key]
        [Display(Name = "Id")]
        [Column(TypeName = "int")]
        [Required(AllowEmptyStrings = false)]
        public int Id { get; set; }


    }
}