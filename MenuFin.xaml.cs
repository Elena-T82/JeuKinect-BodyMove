using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BodyMove
{
	/// <summary>
	/// Logique d'interaction pour MenuFin.xaml
	/// </summary>
	public partial class MenuFin : Window
	{
		KinectSensor MaKinect;

		/// <summary>
		/// Taille d'un pixel en RGB en octets
		/// </summary>
		private int octetsParPixel = (PixelFormats.Bgr32.BitsPerPixel) / 8;

		/// <summary>
		/// Variable pour contenir un frame d'image
		/// </summary>
		private ColorFrameReader lecteurCouleur = null;

		/// <summary>
		/// Un tableau de pixels
		/// </summary>
		private byte[] pixels = null;

		/// <summary>
		/// Une image couleur qui sera liée à l'interface
		/// </summary>
		private WriteableBitmap BitmapCouleur = null;

		/// </summary>
		private Body[] bodies = null;
		//private Body bodies = null;

		/// <summary>
		/// FrameReader pour les joueurs
		/// </summary>
		private BodyFrameReader lecteurBody = null;

		/// <summary>
		/// Indique si le joueur à "cliquer" sur un des boutons proposés
		/// </summary>
		private bool clique = false;

		public MenuFin()
		{
			InitializeComponent();

			/* Chargement de la musique */
			FileInfo info = new FileInfo("Musiques/zMusiqueMenu.mp3");
			musique.Source = new Uri(info.FullName);
			musique.Play();

			// On indique au joueur son score
			lb_Score.Content = "Vous avez réussi " + Joueur.NbPausesReussies + " poses sur " + Joueur.NbPausesDefilees + " poses.";

			DemarrerKinect();
		}

		void DemarrerKinect()
		{
			// Vérification capteurs kinect
			MaKinect = KinectSensor.GetDefault();
			MaKinect.Open();
			if (MaKinect != null)
			{
				initialiserCamera();
				initialiserBody();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (MaKinect != null)
			{
				MaKinect.Close();
			}
		}

		private void initialiserCamera()
		{
			FrameDescription desc = MaKinect.ColorFrameSource.FrameDescription;

			// Obtenir le lecteur d'images
			lecteurCouleur = MaKinect.ColorFrameSource.OpenReader();

			// Allouer le tableau de pixels
			pixels = new byte[desc.Width * desc.Height * octetsParPixel];

			// Créer le bitmap, 96 -> nb pixel par pouce
			BitmapCouleur = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

			// Associer le bitmap à l'image de la fenêtre
			ImageCameraPrincipale.Source = BitmapCouleur;

			// Ajouter l'événement
			lecteurCouleur.FrameArrived += LecteurCouleur_FrameArrived;
		}

		void initialiserBody()
		{
			// Allouer la liste
			bodies = new Body[MaKinect.BodyFrameSource.BodyCount];

			// Ouvrir le lecteur
			lecteurBody = MaKinect.BodyFrameSource.OpenReader();

			// Associer l'événement
			lecteurBody.FrameArrived += lecteurBody_FrameArrived;
		}

		void lecteurBody_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			BodyFrameReference refer = e.FrameReference;

			if (refer == null) return;

			// Obtenir body frame
			BodyFrame frame = refer.AcquireFrame();

			if (frame == null) return;

			using (frame)
			{
				// Obtenir les données des joueurs
				frame.GetAndRefreshBodyData(bodies);

				// Clear Skeleton Canvas
				CanvasCameraPrincipale.Children.Clear();

				List<Point> Mains = new List<Point>();

				// Loop all bodies
				foreach (Body body in bodies)
				{
					// Only process tracked bodies
					if (body.IsTracked && clique == false)
					{
						Mains.Clear();
						Mains.Add(ObtenirPositionEcran(body.Joints[JointType.HandLeft]));
						Mains.Add(ObtenirPositionEcran(body.Joints[JointType.HandRight]));

						//On regarde si un choix à été fait par le joueur
						if (DetermineContact(Mains, bt_menu, body))
						{
							MenuDebut window = new MenuDebut();
							window.Show();
							this.Close();
						}
						else if (DetermineContact(Mains, bt_quitter, body))
						{
							this.Close();
						}
						else if (DetermineContact(Mains, bt_galerie, body))
						{
							Galerie galerie = new Galerie();
							galerie.Show();
							this.Close();
						}
					}
				}
			}
		}

		void LecteurCouleur_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
		{
			// Obtenir la référence du frame couleur
			ColorFrameReference colorRef = e.FrameReference;

			if (colorRef == null) return;

			// Obtenir le frame rattaché à la référence
			ColorFrame frame = colorRef.AcquireFrame();

			// S'assurer qu'on n'est pas entre 2 frames
			if (frame == null) return;

			using (frame)
			{
				// Obtenir la description du frame
				FrameDescription frameDesc = frame.FrameDescription;

				// Vérifier si les dimensions concordent
				if (frameDesc.Width == BitmapCouleur.PixelWidth && frameDesc.Height == BitmapCouleur.PixelHeight)
				{
					// Copier les données selon le format de l'image
					if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
					{
						frame.CopyRawFrameDataToArray(pixels);
					}
					else frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);

					// Copier les données obtenues dans l'image qui est rattachée à l'image d'affichage
					BitmapCouleur.WritePixels(
							new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
							pixels,
							frameDesc.Width * octetsParPixel,
							0);
				}
			}
		}

		private Point ObtenirPositionEcran(Joint p_Joint)
		{
			ColorSpacePoint Point = MaKinect.CoordinateMapper.MapCameraPointToColorSpace(p_Joint.Position);
			//On divise par 1.5 parce qu'on a un écran de 720 pixels par rapport à une image d'origine de 1080 pixels
			return new Point(Point.X / 1.5, Point.Y / 1.5);
		}

		/// <summary>
		/// Fonction permettant de savoir si il y a eu un contact et une main fermée sur un des boutons
		/// </summary>
		/// <param name="p_Main">Points de la main</param>
		/// <param name="pButton">Bouton à vérifier</param>
		/// <param name="pBody"></param>
		/// <returns>Retourne vrai si le joueur ferme la main et est sur le bouton, faux sinon</returns>
		private Boolean DetermineContact(List<Point> p_Main, Button pButton, Body pBody)
		{
			//Permet de localiser un contrôle par rapport à un ancètre, ici, la fenêtre
			GeneralTransform transform = pButton.TransformToAncestor(this);
			Point rootPoint = transform.Transform(new Point(0, 0));
			Point CentreBt = new Point(rootPoint.X, rootPoint.Y);

			bool contact = false;

			//Si la main droite ou gauche est fermée
			if (pBody.HandRightState == HandState.Closed || pBody.HandLeftState == HandState.Closed)
			{
				if (!clique) //On vérifie que le choix de l'action n'est pas déjà été fait pour qu'il n'y ai pas plusieurs fenetres qui s'ouvrent
				{
					// MAIN GAUCHE
					if ((CentreBt.X - (pButton.Width / 2)) < p_Main[0].X && p_Main[0].X < (CentreBt.X + (pButton.Width / 2)))
					{
						if ((CentreBt.Y - (pButton.Height / 2)) < p_Main[0].Y && p_Main[0].Y < (CentreBt.Y + (pButton.Height / 2)))
						{
							contact = true;
							clique = true;
						}
					}

					// MAIN DROITE
					if ((CentreBt.X - (pButton.Width / 2)) < p_Main[1].X && p_Main[1].X < (CentreBt.X + (pButton.Width / 2)))
					{
						if ((CentreBt.Y - (pButton.Height / 2)) < p_Main[1].Y && p_Main[1].Y < (CentreBt.Y + (pButton.Height / 2)))
						{
							contact = true;
							clique = true;
						}
					}
				}
			}
			return contact;
		}

	}
}
