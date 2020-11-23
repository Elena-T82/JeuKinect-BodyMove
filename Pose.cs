using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BodyMove
{
	[System.Serializable]
	class Pose
	{
		private string _lienPhoto;
		private Dictionary<String, int[]> _dicoCoordonnees;

		public Pose(Dictionary<String, int[]> dico, string lien)
		{
			_dicoCoordonnees = dico;
			_lienPhoto = lien;
		}

		public Dictionary<String, int[]> coordonnees
		{
			get { return _dicoCoordonnees; }
		}

		public string photo
		{
			get { return _lienPhoto; }
		}
	}
}
