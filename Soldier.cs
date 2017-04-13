using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barracks_Scanner_WPF
{
    public class Soldier
    {
        public enum Classes { Assault, Grenadier, Gunner, Ranger, Sharpshooter, Shinobi, Specialist, Technical, Psi_Operative}
        public enum Ranks { Rookie, Squaddie, Lance_Corporal, Corporal, Sergeant, Staff_Sergeant, Tech_Sergeant, Gunnery_Sergeant, Master_Sergeant}
        public string Rank { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public int Health { get; set; }
        public int Mobility { get; set; }
        public int Will { get; set; }
        public int Hacking { get; set; }
        public int Dodge { get; set; }
        public int Psi { get; set; }
        public int Aim { get; set; }
        public int Defense { get; set; }

        //public string Status { get; set; }

        public void CorrectErrors()
        {
            if (Rank == "SO")
                Rank = "SQ";
            if (Class == "ASSAU LT")
                Class = "ASSAULT";
        }
    }
}
