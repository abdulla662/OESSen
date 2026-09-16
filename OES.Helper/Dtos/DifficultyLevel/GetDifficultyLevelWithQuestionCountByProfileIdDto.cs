using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.Dtos.DifficultyLevel
{
    public class GetDifficultyLevelWithQuestionCountByProfileIdDto
    {
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public double DifficultyLevelmark { get; set; } = 0;
        public int QuestionCount { get; set; }
    }
}
