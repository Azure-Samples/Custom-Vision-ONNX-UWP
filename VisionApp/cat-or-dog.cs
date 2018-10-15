using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// 5123bece-8ec8-45d6-a0f3-63fcfb00c7e9_ec4c68a4-827f-4850-ae62-0636d7cd015d

namespace VisionApp
{
    public sealed class ModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class ModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IList<IDictionary<string, float>> loss { get; set; }
        public ModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new List<IDictionary<string, float>>();
        }
    }

    public sealed class Model
    {
        private LearningModelPreview learningModel;
        public static async Task<Model> CreateModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            Model model = new Model();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<ModelOutput> EvaluateAsync(ModelInput input) {
            ModelOutput output = new ModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
