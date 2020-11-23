using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BodyMove
{
	class ToutesLesPoses
	{
		//Nombre de poses que contient le jeu
		private int _nbTotalPoses;

		//Liste des poses du jeu
		private List<Pose> _lstPoses;

		public ToutesLesPoses()
		{
			_lstPoses = new List<Pose>();
			lireXml();
		}

		/// <summary>
		/// Rempli la liste des poses grâce à un fichier XML
		/// </summary>
		private void lireXml()
		{
			//On charge le document contenant les poses
			XmlDocument doc = new XmlDocument();
			doc.Load("poses.xml");			

			//On compte le nombre de poses en fonction du nombre de noeuds enfants
			_nbTotalPoses = doc.DocumentElement.ChildNodes.Count;
			
			string lien = "";
			
			//On parcours les noeuds, un noeud ici correspond à une pose
			foreach (XmlNode node in doc.DocumentElement.ChildNodes)
			{
				//string sera le nom du membre du corps et le tableau de int sera les coordonnees x et y
				Dictionary<String, int[]> dico = new Dictionary<string, int[]>();
				
				//On parcours les noeud, un noeud ici correspond à un membre du corps
				foreach (XmlNode node2 in node)
				{
					int[] tab = new int[4];//4 coordonnées par membre
					//On remplit le tableau avec les coordonnées
					switch (node2.Name)
					{
						case "tete":
							tab[0] = Convert.ToInt32(node2.Attributes["x"].InnerText);
							tab[1] = Convert.ToInt32(node2.Attributes["y"].InnerText);
							dico.Add(node2.Name, tab);
							break;

						case "photo":
							lien = node2.Attributes["lien"].InnerText;
							break;

						default:
							tab[0] = Convert.ToInt32(node2.Attributes["x_gauche"].InnerText);
							tab[1] = Convert.ToInt32(node2.Attributes["y_gauche"].InnerText);
							tab[2] = Convert.ToInt32(node2.Attributes["x_droit"].InnerText);
							tab[3] = Convert.ToInt32(node2.Attributes["y_droit"].InnerText);
							dico.Add(node2.Name, tab);
							break;
					}
				}

				//On ajoute la pose à la liste en créant un objet Pose
				_lstPoses.Add(new Pose(dico, lien));
			}
		}

		/// <summary>
		/// Retourne la pose contenue dans la liste à l'index envoyé en paramètre
		/// </summary>
		/// <param name="index">index de la pose à renvoyer</param>
		/// <returns></returns>
		public Pose this[int index]
		{
			get
			{
				if (index >= 0 && index < _lstPoses.Count)
				{
					return _lstPoses[index];
				}

				return null;
			}
		}

		public int nbPoses
		{
			get { return _nbTotalPoses; }
		}
	}
}
