using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.Dtos.Paper.Requests
{
    public class QuestionTypeCountViewRequestDto
    {
        public long ItemBankId { get; set; }

        public string QuestionType { get; set; }

        public string DifficultyLevel { get; set; }

        public int QuestionCount { get; set; }
    }
}
