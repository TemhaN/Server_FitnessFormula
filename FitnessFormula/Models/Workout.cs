namespace FitnessFormula.Models
{
    public class Workout
    {
        public int WorkoutId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; } // <-- Добавили `?`
        public int TrainerId { get; set; }
        public Trainer Trainer { get; set; }
    }


}
