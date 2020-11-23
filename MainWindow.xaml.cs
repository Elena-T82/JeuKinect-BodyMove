using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BodyMove
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
		/// Timer indiquant le temps qu'il reste pour faire la pause
		/// </summary>
		private DispatcherTimer timer = new DispatcherTimer();

		/// <summary>
		/// Variable qui définit le temps pour faire une pause
		/// </summary>
		private int temps = 15;

		/// <summary>
		/// Variable qui contiendra toutes les poses du jeu
		/// </summary>
		ToutesLesPoses lesPoses;

		/// <summary>
		/// Index de la pose s'affichant au début, donc ici 0
		/// </summary>
		private int indexPose = 0;

		/// <summary>
		/// Indique si le joueur a réussi la pose ou non
		/// </summary>
		private bool gagne = false;

		/// <summary>
		/// Indique si le jeu est en pause ou non
		/// </summary>
		private bool pause = false;

		/// <summary>
		/// Obtient les informations sur le dossier de musiques
		/// </summary>
		private DirectoryInfo dossierMusiques;

		/// <summary>
		/// Obtient les informations sur les musiques
		/// </summary>
		private FileInfo[] fichiersMusiques;

		/// <summary>
		/// Index de la musique qui passe
		/// </summary>
		private int indexMusique = 0;

		/// <summary>
		/// Reconnaissance vocale
		/// </summary>
		private SpeechRecognitionEngine engine = new SpeechRecognitionEngine();

		public MainWindow()
		{
			InitializeComponent();

			/* On instancie notre objet pour obtenir toutes les poses*/
			lesPoses = new ToutesLesPoses();

			DemarrerKinect();

			/* On remet les variables statiques du joueur à 0 */
			Joueur.NbPausesDefilees = 0;
			Joueur.NbPausesReussies = 0;

			/* Gestion du timer */
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += timer_Tick;

			/* Gestion de la musique */
			dossierMusiques = new DirectoryInfo("Musiques");
			fichiersMusiques = dossierMusiques.GetFiles();
			//Si il y a bien des musiques dans le dossier on lance la première
			if (fichiersMusiques.Length > 0)
			{
				musique.Source = new Uri(fichiersMusiques[0].FullName);
				musique.Play();
			}

			//On appelle la méthode pour afficher la première pose du jeu
			imgPose.Source = new BitmapImage(new Uri(defilePauses(0)));

			/* Reconnaissance vocale */

			//On entre une liste de mots a reconnaitre (ici nous avons rajouter un mot pour que la reconnaissance soit un peu plus précise, plus il y a de mots plus elle est précise
			Choices choixDeMots = new Choices(new string[] { "licorne", "pause", "jouer" });
			Grammar grammaire = new Grammar(new GrammarBuilder(choixDeMots));
			try
			{
				//On met à jour la reconnaissance
				engine.RequestRecognizerUpdate();
				//On lui attribue les mots a reconnaitre
				engine.LoadGrammar(grammaire);
				//On crée un événement pour quand un des mots de la liste est reconnu
				engine.SpeechRecognized += engine_SpeechRecognized;
				//On définit l'entrée audio par défaut
				engine.SetInputToDefaultAudioDevice();
				//On lance la reconnaissance en asynchrone 
				engine.RecognizeAsync(RecognizeMode.Multiple);
				engine.Recognize();
			}
			catch
			{
				return;
			}
		}

		/// <summary>
		/// Reconnaissance vocale
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			//SpeechRecognitionEngine engine = new SpeechRecognitionEngine();
			try
			{
				//On récupère le mot détecté
				string mot = e.Result.Text;
				
				if(mot == "pause") // Si le mot pause est détecté on arrête le timer ainsi que la musique et on affiche un canvas qui indique que le jeu est en pause
				{
					pause = true;
					menuPause.Visibility = Visibility.Visible;
					timer.Stop();
					musique.Pause();
				}else if(mot == "jouer") // Si le mot jouer est détecté on reprend le timer ainsi que la musique et on cache le canvas
				{
					pause = false;
					menuPause.Visibility = Visibility.Hidden;
					timer.Start();
					musique.Play();

					//engine.RecognizeAsync(RecognizeMode.Multiple);
					//engine.Recognize();
				}
			}
			catch (Exception)
			{
				return;
			}
		}

		/// <summary>
		/// Permet d'obtenir le chemin d'une photo
		/// </summary>
		/// <param name="pIndex">l'index de la pose qu'il faut afficher</param>
		/// <returns>Le chemin absolu de la pose qu'il faut afficher</returns>
		private string defilePauses(int pIndex)
		{
			return System.IO.Path.GetFullPath(lesPoses[indexPose].photo);
		}

		/// <summary>
		/// Gère le défilement de la musique
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void musique_MediaEnded(object sender, RoutedEventArgs e)
		{
			indexMusique++;
			//A la fin d'une musique, si il reste des musique a jouer on joue la prochaine
			if(fichiersMusiques.Length > indexMusique)
			{
				musique.Source = new Uri(fichiersMusiques[indexMusique].FullName);
				musique.Play();
			}
		}
		
		/// <summary>
		/// Gestion du temps
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer_Tick(object sender, EventArgs e)
		{
			//Si le jeu n'est pas en pause on décrémente le timer
			if (pause == false)
			{
				temps--;
			}

			//Si le temps est arrivé à 0, donc que le timer est finit
			if (temps == 0)
			{
				//On remet le temps à 15 secondes 
				temps = 15;

				//On incrémente le nombre de poses qui ont défilées à l'écran
				Joueur.NbPausesDefilees++;

				//On incrémente l'index de la pose pour passer à la pose suivante
				indexPose++;

				//Vérification pour savoir si il reste des poses à afficher
				if (lesPoses.nbPoses > indexPose)
				{
					//Si il reste des poses on affiche la suivante 
					imgPose.Source = new BitmapImage(new Uri(defilePauses(indexPose)));
				}
				else
				{
					//Si il n'y a plus de poses a afficher on ouvre le menu de fin et on ferme la fenêtre de jeu
					MenuFin fin = new MenuFin();
					this.Close();
					fin.Show();					
				}	

				/* Prise de photo du joueur */
				BitmapSource photoJoueur;
				photoJoueur = (BitmapSource)ImageCameraPrincipale.Source;

				//On met la date ainsi que l'heure précise de la prise de la photo comme nom de fichier
				string nomFichier = DateTime.Now.ToString("yyyy-MM-ddHH.mm.ss");

				//Création du fichier 
				using (var fileStream = new FileStream("Images/" + nomFichier + ".bmp", FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(photoJoueur));
					encoder.Save(fileStream);
				}

				//Si pose reussie on incremente la variable statique du joueur indiquant le nombre de poses réussies
				if (gagne)
				{
					Joueur.NbPausesReussies++;
				}
			}

			//On affiche le temps restant à l'écran
			ChronoLabel.Content = temps;	
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
			ImageCameraVignette.Source = BitmapCouleur;

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

			// Obtenir bodies frame
			BodyFrame frame = refer.AcquireFrame();

			if (frame == null) return;

			using (frame)
			{
				// Obtenir les données des joueurs
				frame.GetAndRefreshBodyData(bodies);
				
				// Effacer le canvas
				CanvasCameraPrincipale.Children.Clear();
				CanvasCameraVignette.Children.Clear();

				// Itérer das tous les corps disponibles
				foreach (Body bodies in bodies)
				{
					// On ne traite que les corps trackés
					if (bodies.IsTracked)
					{
						DessineBody(bodies);
					}
				}
				timer.Start();
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

		void DessineBody(Body p_Body)
		{
			//Si une pose est affichée
			if(indexPose < lesPoses.nbPoses)
			{		
				//On récupère les coordonnées de la pose actuelle
				Dictionary<String, int[]> dicoCoordonnees = lesPoses[indexPose].coordonnees;
			
				foreach (JointType type in p_Body.Joints.Keys)
				{
                    
					// Dessiner tous les joints du corps
					switch (type)
					{
						//Pour tous ces point du corps (tête, épaules, coudes, mains, genoux, pieds) il faut vérifier si les points correspondent bien aux points du xml des poses pour savoir si le joueur effectue bien la pose
						case JointType.Head:
							verificationPoints(dicoCoordonnees["tete"][0], dicoCoordonnees["tete"][1], p_Body, type);
							break;
						case JointType.FootLeft:
							verificationPoints(dicoCoordonnees["pieds"][0], dicoCoordonnees["pieds"][1], p_Body, type);
							break;
						case JointType.FootRight:
							verificationPoints(dicoCoordonnees["pieds"][2], dicoCoordonnees["pieds"][3], p_Body, type);
							break;
						case JointType.ShoulderLeft:
							verificationPoints(dicoCoordonnees["epaules"][0], dicoCoordonnees["epaules"][1], p_Body, type);
							break;
						case JointType.ShoulderRight:
							verificationPoints(dicoCoordonnees["epaules"][2], dicoCoordonnees["epaules"][3], p_Body, type);
							break;
						case JointType.ElbowLeft:
							verificationPoints(dicoCoordonnees["coudes"][0], dicoCoordonnees["coudes"][1], p_Body, type);
							break;
						case JointType.ElbowRight:
							verificationPoints(dicoCoordonnees["coudes"][2], dicoCoordonnees["coudes"][3], p_Body, type);
							break;
						case JointType.HandLeft:
							verificationPoints(dicoCoordonnees["mains"][0], dicoCoordonnees["mains"][1], p_Body, type);
							break;
						case JointType.HandRight:
							verificationPoints(dicoCoordonnees["mains"][2], dicoCoordonnees["mains"][3], p_Body, type);
							break;
						case JointType.KneeLeft:
							verificationPoints(dicoCoordonnees["genoux"][0], dicoCoordonnees["genoux"][1], p_Body, type);
							break;
						case JointType.KneeRight:
							verificationPoints(dicoCoordonnees["genoux"][2], dicoCoordonnees["genoux"][3], p_Body, type);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Vérifie les coordonnées envoyées par rapport au XML
		/// </summary>
		/// <param name="pointX">Point d'abscisse correspondant au point XML à atteindre</param>
		/// <param name="pointY">Point d'ordonnée correspondant au point XML à atteindre</param>
		/// <param name="pBody"></param>
		/// <param name="type"></param>
		void verificationPoints(int pointX, int pointY, Body pBody, JointType type)
		{
			//Marge d'erreur qu'à le joueur pour réussir la pose
			int marge = 40;
			int minX = pointX - marge;
			int minY = pointY - marge;
			int maxX = pointX + marge;
			int maxY = pointY + marge;

			ColorSpacePoint colorPoint = MaKinect.CoordinateMapper.MapCameraPointToColorSpace(pBody.Joints[type].Position);

			//Si le point si situe entre les valeurs du xml avec les marges alors la pose est réussie
			if ((minX < colorPoint.X / 1.5 && colorPoint.X / 1.5 < maxX) && (minY < colorPoint.Y / 1.5 && colorPoint.Y / 1.5 < maxY))
			{
				gagne = true;
				//On dessine le point en vert 
				DrawJoint(pBody.Joints[type], 20, Brushes.Green, 2, Brushes.Green);
			}
			else
			{
				gagne = false;
				//On dessine le point en rouge
				DrawJoint(pBody.Joints[type], 20, Brushes.Red, 2, Brushes.Red);
			}
		}

		/// <summary>
		/// Dessine les points du corps du joueur à l'écran
		/// </summary>
		/// <param name="p_Joint"></param>
		/// <param name="p_Rayon"></param>
		/// <param name="p_CouleurFond"></param>
		/// <param name="p_LargeurBordure"></param>
		/// <param name="p_CouleurBordure"></param>
		void DrawJoint(Joint p_Joint, double p_Rayon, SolidColorBrush p_CouleurFond, double p_LargeurBordure, SolidColorBrush p_CouleurBordure)
		{
			if (p_Joint.TrackingState != TrackingState.Tracked) return;

			// Transformer le point de la caméra en point de l'environnement
			ColorSpacePoint colorPoint = MaKinect.CoordinateMapper.MapCameraPointToColorSpace(p_Joint.Position);
			
			// Créer l'ellipse basé sur les paramètres
			Ellipse el = new Ellipse();
			el.Fill = p_CouleurFond;
			el.Stroke = p_CouleurBordure;
			el.StrokeThickness = p_LargeurBordure;
			el.Width = el.Height = p_Rayon;

			// Ajouter l'ellipse sur le canvas
			CanvasCameraPrincipale.Children.Add(el);

			// Éviter le mauvais tracking
			if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.X)) return;

			// ALigner l'ellipse sur le canvas (diviser par 1.5 car l'image originale est en 1080p et sur mon canvas on est en 720p)
			Canvas.SetLeft(el, colorPoint.X / 1.5);
			Canvas.SetTop(el, colorPoint.Y / 1.5);
		}
		
	}
}
