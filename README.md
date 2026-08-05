# 20251220_SSSW
Scan and scale shot weight

Kiến trúc tổng thể:
ShotweightApp
│
├── Core
│   └── Services
│       └── WeightService.cs
│
├── Models
│   └── StepModel.cs
│
├── ViewModels
│   ├── BaseViewModel.cs
│   ├── RelayCommand.cs
│   └── MainViewModel.cs
│
├── Views
│   └── MainWindow.xaml
│
├── Styles
│   └── GlobalStyles.xaml
│
├── App.xaml
└── App.xaml.cs

Chỉnh DeployConfig.props để khi build (release) sẽ tự copy lên thư mục update.