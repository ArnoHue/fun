using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Lonpos {

	public struct Result {
		public bool Success { get; set; }
		public long DurationMs { get; set; }
		public long Combinations { get; set; }
	}

	public class SolutionProgress {
		// public Dictionary<int, long> SkippedPathsPerAvailableFigures = new Dictionary<int, long>();
		public long StepCount { get; set; }
		public long SkippedCount { get; set; }
		public long Milliseconds { get; set; }
		public Pyramid Pyramid { get; set; }
	}

	public class SolutionContext {
		// public Dictionary<int, long> SkippedPathsPerAvailableFigures = new Dictionary<int, long>();
		public bool Done { get; set; }
		public long StepCount { get; set; }
		public long SkippedCount { get; set; }
		public int SolvabilityCheckFigureCntThreshold { get; set; }
		public long PrevCallbackTimestamp { get; set; }
		public Stopwatch Stopwatch { get; set; }
	}

	public interface IProgressCallback {
		void OnProgress(SolutionProgress progress);
	}

	public class LayerPos {
		// direct field access for perf reasons
		public Layer Layer;
		public int X;
		public int Y;
		public Dictionary<Figure, List<FigureMatch>> MatchingFigureDefs = new Dictionary<Figure, List<FigureMatch>>();
	}

	public class Layer {
		// direct field access for perf reasons
		public Sphere[,] Spheres;
		public int Width;
		public int Height;

		public Layer(Sphere[,] spheres) {
			Spheres = spheres;
			Width = spheres.GetLength(0);
			Height = spheres.GetLength(0);
		}

		public Layer(int len) {
			Spheres = new Sphere[len, len];
			Width = len;
			Height = len;
		}

		public void VisitNeighbors(int x, int y, ICollection<Sphere> visited) {
			Sphere sphere = Spheres[x, y];
			if (sphere != null && !sphere.IsSet && !visited.Contains(sphere)) {
				visited.Add(sphere);

				if (x > 0) {
					VisitNeighbors(x - 1, y, visited);
				}
				if (x < Width - 1) {
					VisitNeighbors(x + 1, y, visited);
				}
				if (y > 0) {
					VisitNeighbors(x, y - 1, visited);
				}
				if (y < Height - 1) {
					VisitNeighbors(x, y + 1, visited);
				}

			}
		}

		public void Print() {
			for (int y = 0; y < Height; y++) {
				Console.Write(y + ": ");
				for (int x = 0; x < Width; x++) {
					Console.Write(Spheres[x, y] == null ? "_" : Spheres[x, y].Name);
					Console.Write(" ");
				}
				Console.WriteLine();
			}
		}
	}

	public class FigureDef {
		// direct field access for perf reasons
		public int SphereCount;
		public Sphere[,] Spheres;
		public int Width;
		public int Height;

		public FigureDef(Sphere[,] spheres) {
			Spheres = spheres;
			Width = spheres.GetLength(0);
			Height = spheres.GetLength(1);
			int cnt = 0;
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					Sphere figSphere = Spheres[x, y];
					if (figSphere != null && figSphere.IsSet) {
						cnt++;
					}
				}
			}
			SphereCount = cnt;
		}

		public void Print() {
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					Console.Write(Spheres[x, y] == null ? " " : Spheres[x, y].Name);
					Console.Write(" ");
				}
				Console.WriteLine();
			}
		}

		public bool DoesMatchNoScopeCheck(int xOffset, int yOffset, Layer layer) {
			// code duplicated for perf reasons
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {

					Sphere figSphere = Spheres[x, y];
					if (figSphere != null && figSphere.IsSet) {

						int xPos = xOffset + x;
						int yPos = yOffset + y;

						Sphere sphere = layer.Spheres[xPos, yPos];
						if (sphere == null) {
							return false;
						}
						if (sphere.IsSet) {
							return false;
						}
					}
				}
			}
			return true;
		}

		public bool DoesMatch(int xOffset, int yOffset, Layer layer) {

			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {

					Sphere figSphere = Spheres[x, y];
					if (figSphere != null && figSphere.IsSet) {

						int xPos = xOffset + x;
						int yPos = yOffset + y;
						if (xPos < 0 || xPos >= layer.Width) {
							return false;
						}
						if (yPos < 0 || yPos >= layer.Height) {
							return false;
						}

						Sphere sphere = layer.Spheres[xPos, yPos];
						if (sphere == null) {
							return false;
						}
						if (sphere.IsSet) {
							return false;
						}
					}
				}
			}
			return true;
		}

		public void MergeIntoLayer(int xOffset, int yOffset, Layer layer) {
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					Sphere figSphere = Spheres[x, y];
					if (figSphere != null && figSphere.IsSet) {
						Sphere sphere = layer.Spheres[xOffset + x, yOffset + y];
						sphere.IsSet = true;
						sphere.Name = figSphere.Name;
					}
				}
			}

		}

		public void UnmergeFromLayer(int xOffset, int yOffset, Layer layer) {
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					Sphere figSphere = Spheres[x, y];
					if (figSphere != null && figSphere.IsSet) {
						Sphere sphere = layer.Spheres[xOffset + x, yOffset + y];
						sphere.IsSet = false;
						sphere.Name = "_";
					}
				}
			}
		}
	}

	public struct FigureMatch {
		// direct field access for perf reasons
		public FigureDef FigureDef;
		public int X;
		public int Y;
	}

	public class Sphere {
		// direct field access for perf reasons
		public bool IsSet;
		public List<LayerPos> LayerPositions;

		public string Name { get; set; }
		public Sphere(char name) {
			LayerPositions = new List<LayerPos>();
			Name = name.ToString();
			IsSet = true;
		}
		public static Sphere Create(char name) {
			if (name == ' ') {
				return null;
			} else {
				return new Sphere(name) { IsSet = true };
			}
		}
	}

	public class Figure {
		public int SphereCount { get; private set; }
		private List<FigureDef> rotations;

		public static Figure Create(char[,] def) {

			Sphere[,] spheres = new Sphere[def.GetLength(0), def.GetLength(1)];
			for (int x = 0; x < spheres.GetLength(0); x++) {
				for (int y = 0; y < spheres.GetLength(1); y++) {
					if (def[x, y] != ' ') {
						spheres[x, y] = new Sphere(def[x, y]);
					}
				}
			}
			return new Figure(new FigureDef(spheres));
		}

		public Figure(FigureDef def) {
			SphereCount = def.SphereCount;
			rotations = new List<FigureDef>();
			rotations.Add(def);
			FigureDef def2 = Figure.Flip(def);
			if (!ExistsMutation(def2)) {
				rotations.Add(def2);
			}
			for (int i = 0; i < 4; i++) {
				def = Figure.Rotate(def);
				if (!ExistsMutation(def)) {
					rotations.Add(def);
				}
				def2 = Figure.Rotate(def2);
				if (!ExistsMutation(def2)) {
					rotations.Add(def2);
				}
			}
		}

		private bool ExistsMutation(FigureDef fig) {
			foreach (FigureDef def in rotations) {
				if (AreEqual(fig, def)) {
					return true;
				}
			}
			return false;
		}

		// todo: move these methods to FigureDef
		private static FigureDef Rotate(FigureDef orig) {
			Sphere[,] fig = new Sphere[orig.Height, orig.Width];
			for (int y = 0; y < orig.Width; y++) {
				for (int x = 0; x < orig.Height; x++) {
					fig[orig.Height - 1 - x, y] = orig.Spheres[y, x];
				}
			}
			return new FigureDef(fig);
		}

		private static FigureDef Flip(FigureDef orig) {
			Sphere[,] fig = new Sphere[orig.Width, orig.Height];
			for (int y = 0; y < orig.Height; y++) {
				for (int x = 0; x < orig.Width; x++) {
					fig[orig.Width - 1 - x, y] = orig.Spheres[x, y];
				}
			}
			return new FigureDef(fig);
		}

		private static bool AreEqual(FigureDef fig1, FigureDef fig2) {
			if (fig1.Width != fig2.Width || fig1.Height != fig2.Height) {
				return false;
			}
			for (int x = 0; x < fig1.Width; x++) {
				for (int y = 0; y < fig1.Height; y++) {
					if ((fig1.Spheres[x, y] == null && fig2.Spheres[x, y] != null) || (fig1.Spheres[x, y] != null && fig2.Spheres[x, y] == null)) {
						return false;
					}
				}
			}
			return true;
		}

		public IEnumerable<FigureDef> GetRotations() {
			return rotations.AsEnumerable();
		}

		public ICollection<FigureMatch> GetMatches(int x, int y, Layer layer) {
			List<FigureMatch> matches = new List<FigureMatch>();
			foreach (FigureDef def in rotations) {
				for (int xOff = 0; xOff < def.Width; xOff++) {
					for (int yOff = 0; yOff < def.Height; yOff++) {
						if (def.Spheres[xOff, yOff] != null) {
							if (def.DoesMatch(x - xOff, y - yOff, layer)) {
								def.MergeIntoLayer(x - xOff, y - yOff, layer);
								if (layer.Spheres[x, y].IsSet) {
									matches.Add(new FigureMatch { FigureDef = def, X = xOff, Y = yOff });
								}
								def.UnmergeFromLayer(x - xOff, y - yOff, layer);
							}
						}
					}
				}
			}
			return matches;
		}
	}

	public class Pyramid {
		private List<Layer> mainLayers = new List<Layer>();
		private List<Layer> layers = new List<Layer>();

		public Pyramid() {
			mainLayers.Add(new Layer(5));
			mainLayers.Add(new Layer(4));
			mainLayers.Add(new Layer(3));
			mainLayers.Add(new Layer(2));
			mainLayers.Add(new Layer(1));
			layers.AddRange(mainLayers);

			foreach (Layer layer in layers) {
				for (int x = 0; x < layer.Width; x++) {
					for (int y = 0; y < layer.Height; y++) {
						Sphere sphere = new Sphere('_') { IsSet = false };
						layer.Spheres[x, y] = sphere;
						sphere.LayerPositions.Add(new LayerPos { Layer = layer, X = x, Y = y });

					}
				}
			}

			// crosscutting layers run parallel to the pyramid's diagonals. they allow figures to be placed in z direction
			// only the inner 3 crosscutting layers can contain figures, the 2 corner layers can't (either too small, or would crop off a single sphere space, which again is too small to hold figures)
			// layerIdx is the distance from the pyramid center
			for (int layerIdx = 0; layerIdx < 3; layerIdx++) {
				Layer layer1 = new Layer(5);
				Layer layer2 = new Layer(5);
				Layer layer3 = new Layer(5);
				Layer layer4 = new Layer(5);

				layers.Add(layer1);
				layers.Add(layer2);
				layers.Add(layer3);
				layers.Add(layer4);

				// calculate positions of existing spheres within crosscutting layers; basically that implies a 45deg rotation
				for (int pyramidIdx = 0; pyramidIdx < mainLayers.Count; pyramidIdx++) {
					Layer layer = mainLayers[pyramidIdx];
					for (int pyrX = 0; pyrX < layer.Width; pyrX++) {
						for (int pyrY = 0; pyrY < layer.Height; pyrY++) {
							int pyrLen = layer.Width;
							if (pyrLen > pyrX && pyrLen > pyrY) {
								Sphere sphere = layer.Spheres[pyrX, pyrY];

								if (pyrX - pyrY == layerIdx) {
									layer1.Spheres[pyrX - layerIdx, pyramidIdx + pyrY] = sphere;
									sphere.LayerPositions.Add(new LayerPos { Layer = layer1, X = pyrX - layerIdx, Y = pyramidIdx + pyrY });
								}
								if (pyrY - pyrX == layerIdx) {
									layer2.Spheres[pyrX, pyramidIdx + pyrY] = sphere;
									// prevent double definition of central diagonal layer
									if (layerIdx > 0) {
										sphere.LayerPositions.Add(new LayerPos { Layer = layer2, X = pyrX, Y = pyramidIdx + pyrY });
									}
								}
								if (pyrX + pyrY == pyrLen - 1 - layerIdx) {
									layer3.Spheres[pyrY, pyramidIdx + pyrY] = sphere;
									sphere.LayerPositions.Add(new LayerPos { Layer = layer3, X = pyrY, Y = pyramidIdx + pyrY });
								}
								if (pyrX + pyrY == pyrLen - 1 + layerIdx) {
									layer4.Spheres[pyrY - layerIdx, pyramidIdx + pyrY] = sphere;
									// prevent double definition of central diagonal layer
									if (layerIdx > 0) {
										sphere.LayerPositions.Add(new LayerPos { Layer = layer4, X = pyrY - layerIdx, Y = pyramidIdx + pyrY });
									}
								}

							}
						}
					}

				}
			}
		}

		public void Print() {
			foreach (Layer layer in mainLayers) {
				layer.Print();
				Console.WriteLine();
			}
		}

		public void MergeIntoLayer(FigureDef def, int layerIdx, int x, int y) {
			def.MergeIntoLayer(x, y, mainLayers[layerIdx]);
		}

		private bool IsSolutionPossible(ICollection<Figure> availableFigures) {
			if (availableFigures.Count == 0) {
				return true;
			}
			// int maxSphereCount = availableFigures.OrderByDescending(f => f.SphereCount).First().SphereCount;
			int minSphereCount = availableFigures.OrderBy(f => f.SphereCount).First().SphereCount;
			HashSet<Sphere> allVisited = new HashSet<Sphere>();
			HashSet<Sphere> visited = new HashSet<Sphere>();
			foreach (Layer layer in mainLayers) {
				for (int x = 0; x < layer.Width; x++) {
					for (int y = 0; y < layer.Height; y++) {
						Sphere sphere = layer.Spheres[x, y];
						if (sphere != null && !sphere.IsSet && !allVisited.Contains(sphere)) {
							bool found = false;
							foreach (LayerPos lp in sphere.LayerPositions) {
								visited.Clear();
								lp.Layer.VisitNeighbors(lp.X, lp.Y, visited);
								if (visited.Count >= minSphereCount) {
									foreach (Sphere v in visited) {
										allVisited.Add(v);
									}
									found = true;
									break;
								}
							}
							if (!found) {
								return false;
							}
						}
					}
				}
			}
			return true;
		}

		private Layer GetLowestUnfinishedLayer() {
			foreach (Layer layer in mainLayers) {
				for (int x = 0; x < layer.Width; x++) {
					for (int y = 0; y < layer.Height; y++) {
						Sphere sphere = layer.Spheres[x, y];
						if (!sphere.IsSet) {
							return layer;
						}
					}
				}
			}
			return null;
		}

		public Result Solve(IList<Figure> figures, IProgressCallback cb) {
			foreach (Layer layer in mainLayers) {
				for (int x = 0; x < layer.Width; x++) {
					for (int y = 0; y < layer.Height; y++) {
						Sphere sphere = layer.Spheres[x, y];
						foreach (LayerPos lp in sphere.LayerPositions) {
							foreach (Figure fig in figures) {
								List<FigureMatch> matches = new List<FigureMatch>();
								matches.AddRange(fig.GetMatches(lp.X, lp.Y, lp.Layer));
								lp.MatchingFigureDefs[fig] = matches;
							}
						}
					}
				}
			}

			Stopwatch watch = new Stopwatch();
			watch.Start();

			SolutionContext ctx = new SolutionContext();
			ctx.Stopwatch = watch;
			ctx.PrevCallbackTimestamp = 0;
			ctx.SolvabilityCheckFigureCntThreshold = 7;
			ctx.Done = false;

			SolveStep(figures, cb, ctx);
			watch.Stop();
			return new Result { Success = ctx.Done, DurationMs = watch.ElapsedMilliseconds, Combinations = ctx.StepCount + ctx.SkippedCount };
		}

		// solution implementation by recursion
		private void SolveStep(IList<Figure> figures, IProgressCallback cb, SolutionContext ctx) {

			ctx.StepCount++;
			if (figures.Count == 0) {
				ctx.Done = true;
				return;
			}
			if (ctx.Stopwatch.ElapsedMilliseconds - ctx.PrevCallbackTimestamp > 1000) {
				ctx.PrevCallbackTimestamp = ctx.Stopwatch.ElapsedMilliseconds;
				cb.OnProgress(new SolutionProgress { Milliseconds = ctx.Stopwatch.ElapsedMilliseconds, StepCount = ctx.StepCount, Pyramid = this, SkippedCount = ctx.SkippedCount /*, SkippedPathsPerAvailableFigures = ctx.SkippedPathsPerAvailableFigures */ });
			}

			List<FigureMatch> matches = new List<FigureMatch>();
			HashSet<Sphere[,]> mutations = new HashSet<Sphere[,]>();

			// we work bottom up the pyramid
			Layer layer = GetLowestUnfinishedLayer();

			// find all free positions in pyramid
			for (int x = 0; x < layer.Width; x++) {
				for (int y = 0; y < layer.Height; y++) {

					Sphere sphere = layer.Spheres[x, y];
					if (!sphere.IsSet) {

						// iterate over figures not yet placed
						for (int figIdx = figures.Count - 1; figIdx >= 0; figIdx--) {
							Figure fig = figures[figIdx];

							foreach (LayerPos lp in sphere.LayerPositions) {

								// iterate over precalculated potential figure rotation matches for that position
								foreach (FigureMatch match in lp.MatchingFigureDefs[fig]) {
									FigureDef figDef = match.FigureDef;
									int matchX = lp.X - match.X;
									int matchY = lp.Y - match.Y;
									Layer matchLayer = lp.Layer;

									// check if figure rotation still matches given the current filling
									if (figDef.DoesMatchNoScopeCheck(matchX, matchY, matchLayer)) {

										// place figure into pyramid
										figDef.MergeIntoLayer(matchX, matchY, matchLayer);

										figures.Remove(fig);
										bool solutionPossible = true;
										// if the pyramid is full enough, we check if any isolated holes already prevent solution, so we don't even go that path
										if (figures.Count <= ctx.SolvabilityCheckFigureCntThreshold) {
											solutionPossible = IsSolutionPossible(figures);
										}
										if (solutionPossible) {
											// run next round with figure placed
											SolveStep(figures, cb, ctx);
											if (ctx.Done) {
												return;
											}
										} else {
											ctx.SkippedCount = ctx.SkippedCount + 1;
											//if (!ctx.SkippedPathsPerAvailableFigures.ContainsKey(figures.Count)) {
											//    ctx.SkippedPathsPerAvailableFigures[figures.Count] = 0;
											//}
											//ctx.SkippedPathsPerAvailableFigures[figures.Count] = ctx.SkippedPathsPerAvailableFigures[figures.Count] + 1;
										}
										figures.Add(fig);
										figDef.UnmergeFromLayer(matchX, matchY, matchLayer);
									}

								}

							}
						}
					}
				}
			}

		}

	}

	class ProgressCallback : IProgressCallback {
		public void OnProgress(SolutionProgress progress) {
			Console.WriteLine(progress.StepCount + " combinations calculated, " + progress.SkippedCount + " skipped in " + (progress.Milliseconds / 1000) + " sec");
			//foreach (int key in progress.SkippedPathsPerAvailableFigures.Keys) {
			//    Console.WriteLine(progress.SkippedPathsPerAvailableFigures[key] + " path skipped for " + key +" available figures");
			//}
			progress.Pyramid.Print();
		}
	}

	class Program {

		static Figure green = Figure.Create(new char[,] {
				{ ' ', ' ', 'E', 'E' },
				{ 'E', 'E', 'E', ' ' }
				}
		);
		static Figure white = Figure.Create(new char[,] {
				{ 'F', 'F' },
				{ 'F', ' ' }
				}
		);
		static Figure lightBlue = Figure.Create(new char[,] {
				{ 'G', ' ', ' ' },
				{ 'G', ' ', ' ' },
				{ 'G', 'G', 'G' }
				}
		);
		static Figure blue = Figure.Create(new char[,] {
				{ 'C', 'C', 'C', 'C' },
				{ 'C', ' ', ' ', ' ' }
			}
		);
		static Figure purple = Figure.Create(new char[,] {
				{ 'J', 'J', 'J', 'J' }
			}
		);
		static Figure red = Figure.Create(new char[,] {
				{ 'B', 'B', 'B' },
				{ ' ', 'B', 'B' }
			}
		);
		static Figure orange = Figure.Create(new char[,] {
				{ ' ', 'A' },
				{ ' ', 'A' },
				{ 'A', 'A' }
			}
		);
		static Figure lightPink = Figure.Create(new char[,] {
				{ ' ', 'D', ' ', ' ' },
				{ 'D', 'D', 'D', 'D' }
			}
		);

		static Figure yellow = Figure.Create(new char[,] {
				{ 'I', ' ', 'I' },
				{ 'I', 'I', 'I' }
			}
		);
		static Figure lightGreen = Figure.Create(new char[,] {
				{ 'K', 'K' },
				{ 'K', 'K' }
			}
		);
		static Figure pink = Figure.Create(new char[,] {
				{ 'H', 'H', ' ' },
				{ ' ', 'H', 'H' },
				{ ' ', ' ', 'H' }
				}
		);
		static Figure gray = Figure.Create(new char[,] {
				{ ' ', 'L', ' ' },
				{ 'L', 'L', 'L' },
				{ ' ', 'L', ' ' }
			}
		);

		static void Main(string[] args) {

			List<Figure> figures = new List<Figure>();

			figures.Add(green);
			figures.Add(white);
			figures.Add(lightBlue);
			figures.Add(blue);
			figures.Add(purple);
			figures.Add(red);
			figures.Add(orange);
			figures.Add(lightPink);
			figures.Add(yellow);
			figures.Add(lightGreen);
			figures.Add(pink);
			figures.Add(gray);

			int i = 0;
			int j = 0;
			foreach (Figure fig in figures) {
				Console.WriteLine("==========");
				Console.WriteLine("Figure " + i + ":");
				Console.WriteLine("==========");
				foreach (FigureDef figDef in fig.GetRotations()) {
					Console.WriteLine("Idx[" + j + "]");
					figDef.Print();
					Console.WriteLine();
					j++;
				}
				j = 0;
				i++;
				Console.WriteLine();
			}

			Solve(figures);
		}

		private static void Solve(IList<Figure> figures) {
			List<Figure> availFigures = new List<Figure>();
			availFigures.AddRange(figures);

			Pyramid pyramid = new Pyramid();

            // wizard level / #97
            // solved in 0,03 sec
            //pyramid.MergeIntoLayer(pink.GetRotations().ElementAt(1), 0, 0, 0);
            //availFigures.Remove(pink);
            //pyramid.MergeIntoLayer(white.GetRotations().ElementAt(0), 0, 0, 0);
            //availFigures.Remove(white);
            //pyramid.MergeIntoLayer(red.GetRotations().ElementAt(2), 1, 0, 0);
            //availFigures.Remove(red);
            //pyramid.MergeIntoLayer(lightPink.GetRotations().ElementAt(5), 0, 2, 0);
            //availFigures.Remove(lightPink);
            //pyramid.MergeIntoLayer(blue.GetRotations().ElementAt(4), 0, 3, 1);
            //availFigures.Remove(blue);

            // wizard level / #98
            // solved in 0,49 sec
            //pyramid.MergeIntoLayer(green.GetRotations().ElementAt(2), 0, 0, 3);
            //pyramid.MergeIntoLayer(red.GetRotations().ElementAt(5), 0, 0, 0);
            //pyramid.MergeIntoLayer(purple.GetRotations().ElementAt(0), 0, 4, 0);
            //pyramid.MergeIntoLayer(yellow.GetRotations().ElementAt(1), 0, 2, 0);
            //availFigures.Remove(green);
            //availFigures.Remove(red);
            //availFigures.Remove(purple);
            //availFigures.Remove(yellow);

            // wizard level / #99
            // solved in 0,10 sec
            //pyramid.MergeIntoLayer(orange.GetRotations().ElementAt(3), 0, 0, 0);
            //pyramid.MergeIntoLayer(red.GetRotations().ElementAt(1), 0, 1, 0);
            //pyramid.MergeIntoLayer(lightGreen.GetRotations().ElementAt(0), 0, 0, 3);
            //availFigures.Remove(orange);
            //availFigures.Remove(red);
            //availFigures.Remove(lightGreen);

            // wizard level / #100
            // solved in ...
            //pyramid.MergeIntoLayer(red.GetRotations().ElementAt(7), 0, 2, 2);
            //availFigures.Remove(red);
            //pyramid.MergeIntoLayer(yellow.GetRotations().ElementAt(3), 0, 2, 0);
            //availFigures.Remove(yellow);

            // wizard level / #101
            // solved in ...
            pyramid.MergeIntoLayer(green.GetRotations().ElementAt(5), 0, 3, 0);
            pyramid.MergeIntoLayer(red.GetRotations().ElementAt(2), 0, 0, 0);
            availFigures.Remove(green);
            availFigures.Remove(red);

            // found online, that's one cool pyramid
            // solved in 0,58 sec
            //pyramid.MergeIntoLayer(orange.GetRotations().ElementAt(1), 0, 0, 3);
            //pyramid.MergeIntoLayer(green.GetRotations().ElementAt(6), 0, 1, 3);
            //pyramid.MergeIntoLayer(red.GetRotations().ElementAt(3), 0, 0, 1);
            //pyramid.MergeIntoLayer(purple.GetRotations().ElementAt(1), 0, 0, 0);
            //pyramid.MergeIntoLayer(lightPink.GetRotations().ElementAt(0), 0, 3, 0);
            //availFigures.Remove(orange);
            //availFigures.Remove(green);
            //availFigures.Remove(red);
            //availFigures.Remove(purple);
            //availFigures.Remove(lightPink);

            pyramid.Print();

			Console.WriteLine("Press key to start");
			Console.ReadKey();
			Result res = pyramid.Solve(availFigures, new ProgressCallback());
			if (res.Success) {
				Console.WriteLine("Found solution in " + res.DurationMs + " ms, " + res.Combinations + " combinationes tried");
				pyramid.Print();
			} else {
				Console.WriteLine("Did not find any solution in " + res.DurationMs + " ms, " + res.Combinations + " combinationes tried");
				pyramid.Print();
			}
			Console.WriteLine("Press key to exit");
			Console.ReadKey();
		}

	}
}
