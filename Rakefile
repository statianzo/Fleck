require 'rubygems'
require 'albacore'

load 'support/platform.rb'
load 'support/buildUtils.rb'
load 'VERSION.txt'

ROOT_NAMESPACE = 'Fleck'
RESULTS_DIR = 'build/test-reports'
PRODUCT = ROOT_NAMESPACE
COPYRIGHT = 'Copyright Jason Staten 2010-2011. All rights reserved.';
COMMON_ASSEMBLY_INFO = 'src/CommonAssemblyInfo.cs';
CLR_VERSION = 'v4.0'
COMPILE_TARGET = 'Debug'
CLR_TOOLS_VERSION = 'v4.0.30319'

props = { :archive => 'artifacts', :stage => 'stage' }

desc 'Compiles and runs unit tests'
task :all => [:default]

desc '**Default**, compiles and runs tests'
task :default => [:compile, :test]

desc 'Update the version information for the build'
assemblyinfo :version do |asm|
  tc_build_number = ENV['BUILD_NUMBER']
  build_revision = tc_build_number || Time.new.strftime('5%H%M')
  BUILD_NUMBER = "#{BUILD_VERSION}.#{build_revision}"

  asm_version = "#{BUILD_VERSION}.0"

  begin
    commit = `git log -1 --pretty=format:%H`
  rescue
    commit = "git unavailable"
  end

  puts "##teamcity[buildNumber '#{BUILD_NUMBER}']" unless tc_build_number.nil?
  puts "Version: #{BUILD_NUMBER}" if tc_build_number.nil?
  asm.trademark = commit
  asm.product_name = PRODUCT
  asm.description = BUILD_NUMBER
  asm.version = asm_version
  asm.file_version = BUILD_NUMBER
  asm.custom_attributes :AssemblyInformationalVersion => asm_version
  asm.copyright = COPYRIGHT
  asm.output_file = COMMON_ASSEMBLY_INFO
end

desc 'Prepares the working directory for a new build'
task :clean do
  puts('recreating the build directory')
  buildDir = props[:archive]
  rm_r buildDir if Dir.exists?(buildDir)
  rm_r RESULTS_DIR  if Dir.exists?(RESULTS_DIR)
  mkdir_p buildDir
  mkdir_p RESULTS_DIR
end

desc 'Compiles the app'
task :compile => [:clean, :version] do
  MSBuildRunner.compile :compilemode => COMPILE_TARGET,
                        :solutionfile => "src/#{ROOT_NAMESPACE}.sln",
                        :clrversion => CLR_TOOLS_VERSION
  copyOutputFiles "src/#{ROOT_NAMESPACE}/bin/#{COMPILE_TARGET}", "*.{dll,pdb}", props[:archive]
end

desc 'Runs unit tests'
task :test do
  runner = NUnitRunner.new :compilemode => COMPILE_TARGET,
                           :source => 'src',
                           :platform => 'x86',
                           :results => RESULTS_DIR

  runner.executeTests ['Fleck.Tests']
end

def copyOutputFiles(fromDir, filePattern, outDir)
  copy Dir[File.join(fromDir, filePattern)], outDir
end
