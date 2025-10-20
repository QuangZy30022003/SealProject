namespace Repositories.Models
{
    public partial class CriterionDetail
    {
        public int CriterionDetailId { get; set; }

        public int CriteriaId { get; set; }

        public string Description { get; set; } = null!;

        public int MaxScore { get; set; }

        public virtual Criterion Criterion { get; set; } = null!;
    }

}