plugins {
	id("com.android.library") version "8.2.2"
	id("org.jetbrains.kotlin.android") version "1.9.22"
}

android {
	namespace = "com.example.mobiletestgame.gpgs"
	compileSdk = 34

	defaultConfig {
		minSdk = 24
	}

	compileOptions {
		sourceCompatibility = JavaVersion.VERSION_17
		targetCompatibility = JavaVersion.VERSION_17
	}
	kotlinOptions {
		jvmTarget = "17"
	}
}

dependencies {
	compileOnly("org.godotengine:godot:4.3.0.stable")
}
