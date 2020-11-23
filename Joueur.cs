using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BodyMove
{
	public class Joueur
	{
		//Variables static pour ne pas avoir à créer un objet joueur sachant qu'il n'existe qu'un seul joueur durant la partie
		static private int _nbPosesReussies;
		static private int _nbPosesDefilees;
		
		public Joueur()
		{
			_nbPosesDefilees = 0;
			_nbPosesReussies = 0;
		}

		static public int NbPausesReussies
		{
			get { return _nbPosesReussies; }
			set { _nbPosesReussies = value; }
		}

		static public int NbPausesDefilees
		{
			get { return _nbPosesDefilees; }
			set { _nbPosesDefilees = value; }
		}

	}
}
