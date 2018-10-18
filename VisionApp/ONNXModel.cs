using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Storage;

namespace VisionApp
{
    /// <summary>
    /// Model input
    /// </summary>
    class ONNXModelInput
    {
        // VideoFrame from the camera
        public VideoFrame data { get; set; }
    }

    /// <summary>
    /// Model output
    /// </summary>
    class ONNXModelOutput
    {
        // The label returned by the model
        public TensorString classLabel = TensorString.Create(new long[] { 1, 1 });
        // The loss returned by the model
        public IList<IDictionary<string, float>> loss = new List<IDictionary<string, float>>();
    }

    /// <summary>
    /// Class for working with the ONNX file
    /// </summary>
    class ONNXModel
    {
        private LearningModel _learningModel = null;
        private LearningModelSession _session;

        // Create a model from an ONNX 1.2 file
        public static async Task<ONNXModel> CreateONNXModel(StorageFile file)
        {
            LearningModel learningModel = null;
            try
            {
                learningModel = await LearningModel.LoadFromStorageFileAsync(file);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                throw e;
            }
            return new ONNXModel()
            {
                _learningModel = learningModel,
                _session = new LearningModelSession(learningModel)
            };
        }
        
        /// <summary>
        /// Evaluate the model
        /// </summary>
        /// <param name="input">The VideoFrame to evaluate</param>
        /// <returns></returns>
        public async Task<ONNXModelOutput> EvaluateAsync(ONNXModelInput input)
        {
            var output = new ONNXModelOutput();
            var binding = new LearningModelBinding(_session);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResult result = await _session.EvaluateAsync(binding, "0");
            return output;
        }
    }
}
