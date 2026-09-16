using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.Dtos.QTI
{
    public class AssessmentChoices
    {
        public long AssessmentChoicesIdentifier { get; set; }
        public string AssessmentChoicesContent { get; set; }
        public bool AssessmentChoicesIsCorrect { get; set; }
    }
}
